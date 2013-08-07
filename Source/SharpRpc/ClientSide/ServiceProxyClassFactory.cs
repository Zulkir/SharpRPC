#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SharpRpc.Codecs;
using SharpRpc.Reflection;
using System.Linq;
using SharpRpc.Utility;

namespace SharpRpc.ClientSide
{
    public class ServiceProxyClassFactory : IServiceProxyClassFactory
    {
        struct ParameterNecessity
        {
            public MethodParameterDescription Description;
            public IEmittingCodec Codec;
            public int ArgumentIndex;
        }

        private readonly IServiceDescriptionBuilder serviceDescriptionBuilder;
        private readonly ICodecContainer codecContainer;
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;

        public ServiceProxyClassFactory(IServiceDescriptionBuilder serviceDescriptionBuilder, ICodecContainer codecContainer)
        {
            this.serviceDescriptionBuilder = serviceDescriptionBuilder;
            this.codecContainer = codecContainer;
            var appDomain = AppDomain.CurrentDomain;
            assemblyBuilder = appDomain.DefineDynamicAssembly(new AssemblyName("SharpRpcServiceProxies"), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("SharpRpcServiceProxyModule");
        }

        private static readonly Type[] ConstructorParameterTypes = new[] { typeof(IOutgoingMethodCallProcessor), typeof(string) };
        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");
        private static readonly MethodInfo ProcessMethod = typeof(IOutgoingMethodCallProcessor).GetMethod("Process");

        public Func<IOutgoingMethodCallProcessor, string, T> CreateProxyClass<T>()
        {
            var proxyClass = CreateProxyClass(typeof(T), null);
            return (p, s) => (T) Activator.CreateInstance(proxyClass, p, s);
        }

        private Type CreateProxyClass(Type type, string path)
        {
            var serviceDescription = serviceDescriptionBuilder.Build(type);
            path = path ?? serviceDescription.Name;

            var typeBuilder = moduleBuilder.DefineType("__rpc_proxy_" + type.FullName,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, 
                typeof(object), new[] {type});

            #region Emit Fields
            var processorField = typeBuilder.DefineField("methodCallProcessor", typeof(IOutgoingMethodCallProcessor), 
                FieldAttributes.Private | FieldAttributes.InitOnly);
            var scopeField = typeBuilder.DefineField("scope", typeof(string),
                FieldAttributes.Private | FieldAttributes.InitOnly);
            #endregion

            #region Begin Emit Constructor
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard, ConstructorParameterTypes);
            var baseConstructor = typeof(object).GetConstructor(Type.EmptyTypes);
            var cil = constructorBuilder.GetILGenerator();
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Call, baseConstructor);
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldarg_1);
            cil.Emit(OpCodes.Stfld, processorField);
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldarg_2);
            cil.Emit(OpCodes.Stfld, scopeField);
            #endregion

            foreach (var subserviceDesc in serviceDescription.Subservices)
            {
                #region Emit Subservice Property
                var proxyClass = CreateProxyClass(subserviceDesc.Service.Type, path + "/" + subserviceDesc.Name);

                var fieldBuilder = typeBuilder.DefineField("_" + subserviceDesc.Name, proxyClass, 
                    FieldAttributes.Private | FieldAttributes.InitOnly);

                cil.Emit(OpCodes.Ldarg_0);
                cil.Emit(OpCodes.Ldarg_1);
                cil.Emit(OpCodes.Ldarg_2);
                cil.Emit(OpCodes.Newobj, proxyClass.GetConstructor(ConstructorParameterTypes));
                cil.Emit(OpCodes.Stfld, fieldBuilder);

                var methodBuilder = typeBuilder.DefineMethod("get_" + subserviceDesc.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    subserviceDesc.Service.Type, Type.EmptyTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);
                var il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldBuilder);
                il.Emit(OpCodes.Ret);

                var propertyBuilder = typeBuilder.DefineProperty(subserviceDesc.Name, 
                    PropertyAttributes.None, subserviceDesc.Service.Type, Type.EmptyTypes);
                propertyBuilder.SetGetMethod(methodBuilder);
                #endregion
            }

            #region End Emit Constructor
            cil.Emit(OpCodes.Ret);
            #endregion

            foreach (var methodDesc in serviceDescription.Methods)
            {
                #region Emit Method
                var parameterTypes = methodDesc.Parameters.Select(x => x.Way == MethodParameterWay.Val ? x.Type : x.Type.MakeByRefType()).ToArray();
                var dynamicMethod = EmitDynamicMethod(type, path, methodDesc, parameterTypes, codecContainer);
                var dynamicMethodPointer = MethodHelpers.ExtractDynamicMethodPointer(dynamicMethod);

                var methodBuilder = typeBuilder.DefineMethod(methodDesc.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    dynamicMethod.ReturnType, parameterTypes);
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, processorField);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, scopeField);
                for (int i = 0; i < methodDesc.Parameters.Count; i++)
                    il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit_Ldc_IntPtr(dynamicMethodPointer);
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, 
                    dynamicMethod.ReturnType, 
                    dynamicMethod.GetParameters().Select(x => x.ParameterType).ToArray(), 
                    null);
                il.Emit(OpCodes.Ret);
                #endregion
            }

            return typeBuilder.CreateType();
        }

        private static readonly Type[] ProcessorAndScopeParamTypes = new[] { typeof(IOutgoingMethodCallProcessor), typeof(string) };

        private static DynamicMethod EmitDynamicMethod(Type type, string path, MethodDescription methodDesc, IEnumerable<Type> realParameterTypes, ICodecContainer codecContainer)
        {
            var dynamicMethod = new DynamicMethod("__dynproxy_" + path + "." + methodDesc,
                methodDesc.ReturnType,
                ProcessorAndScopeParamTypes.Concat(realParameterTypes).ToArray(),
                Assembly.GetExecutingAssembly().ManifestModule, true);

            var il = dynamicMethod.GetILGenerator();

            var parameters = methodDesc.Parameters.Select((x, i) => new ParameterNecessity
            {
                Description = x,
                Codec = codecContainer.GetEmittingCodecFor(x.Type),
                ArgumentIndex = i + 2
            }).ToArray();

            var requestParameters = parameters
                .Where(x => x.Description.Way == MethodParameterWay.Val || x.Description.Way == MethodParameterWay.Ref)
                .ToArray();

            var responseParameters = parameters
                .Where(x => x.Description.Way == MethodParameterWay.Ref || x.Description.Way == MethodParameterWay.Out)
                .ToArray();

            bool hasRetval = methodDesc.ReturnType != typeof(void);
            var locals = new LocalVariableCollection(il, responseParameters.Any() || hasRetval);

            var dataArrayVar = il.DeclareLocal(typeof(byte[]));        // byte[] dataArray

            if (requestParameters.Any())
            {
                bool hasSizeOnStack = false;
                foreach (var parameter in requestParameters)
                {
                    switch (parameter.Description.Way)
                    {
                        case MethodParameterWay.Val:
                            parameter.Codec.EmitCalculateSize(         // stack_0 += calculateSize(arg_i+1) 
                                il, parameter.ArgumentIndex);
                            break;
                        case MethodParameterWay.Ref:
                            parameter.Codec.EmitCalculateSizeIndirect( // stack_0 += calculateSizeIndirect(arg_i+1)
                                il, parameter.ArgumentIndex, parameter.Description.Type);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (hasSizeOnStack)
                        il.Emit(OpCodes.Add);
                    else
                        hasSizeOnStack = true;
                }

                il.Emit(OpCodes.Newarr, typeof(byte));                // dataArray = new byte[stack_0]
                il.Emit(OpCodes.Stloc, dataArrayVar);
                var dataPointerVar =                                  // var pinned dataPointer = pin(dataArray)
                    il.Emit_PinArray(typeof(byte), locals, dataArrayVar);
                il.Emit(OpCodes.Ldloc, dataPointerVar);               // data = dataPointer
                il.Emit(OpCodes.Stloc, locals.DataPointer);

                foreach (var parameter in requestParameters)
                {
                    switch (parameter.Description.Way)
                    {
                        case MethodParameterWay.Val:
                            parameter.Codec.EmitEncode(              // encode(arg_i+1)
                                il, locals, parameter.ArgumentIndex);
                            break;
                        case MethodParameterWay.Ref:
                            parameter.Codec.EmitEncodeIndirect(     // encodeIndirect (arg_i+1)
                                il, locals, parameter.ArgumentIndex, parameter.Description.Type);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                il.Emit_UnpinArray(dataPointerVar);                 // unpin(dataPointer)
            }
            else
            {
                il.Emit(OpCodes.Ldnull);                            // dataArray = null
                il.Emit(OpCodes.Stloc, dataArrayVar);
            }

            il.Emit(OpCodes.Ldarg_0);                               // stack_0 = methodCallProcessor
            il.Emit(OpCodes.Ldtoken, type);                         // stack_1 = typeof(T)
            il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            il.Emit(OpCodes.Ldstr, string.Format("{0}/{1}",         // stack_2 = SuperServicePath/ServiceName/MethodName
                path, methodDesc.Name));
            il.Emit(OpCodes.Ldarg_1);                               // stack_3 = scope
            il.Emit(OpCodes.Ldloc, dataArrayVar);                   // stack_4 = dataArray
            il.Emit(OpCodes.Callvirt, ProcessMethod);               // stack_0 = stack_0.Process(stack_1, stack_2, stack_3, stack_4)

            if (responseParameters.Any() || hasRetval)
            {
                il.Emit(OpCodes.Stloc, dataArrayVar);               // dataArray = stack_0
                il.Emit(OpCodes.Ldloc, dataArrayVar);               // remainingBytes = dataArray.Length
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Stloc, locals.RemainingBytes);
                var dataPointerVar =                                // var pinned dataPointer = pin(dataArray)
                    il.Emit_PinArray(typeof(byte), locals, dataArrayVar);
                il.Emit(OpCodes.Ldloc, dataPointerVar);             // data = dataPointer
                il.Emit(OpCodes.Stloc, locals.DataPointer);

                foreach (var parameter in responseParameters)
                {
                    il.Emit(OpCodes.Ldarg, parameter.ArgumentIndex);// arg_i+1 = decode(data, remainingBytes, false)
                    parameter.Codec.EmitDecode(il, locals, false);
                    il.Emit_Stind(parameter.Description.Type);
                }

                if (hasRetval)
                {
                    var retvalCodec = codecContainer.GetEmittingCodecFor(methodDesc.ReturnType);
                    retvalCodec.EmitDecode(il, locals, false);      // stack_0 = decode(data, remainingBytes, false)
                }

                il.Emit_UnpinArray(dataPointerVar);                 // unpin(dataPointer)
            }
            else
            {
                il.Emit(OpCodes.Pop);                               // pop(stack_0)
            }

            il.Emit(OpCodes.Ret);

            return dynamicMethod;
        }
    }
}

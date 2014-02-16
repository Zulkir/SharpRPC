#region License
/*
Copyright (c) 2013-2014 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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
using System.Threading;
using SharpRpc.Codecs;
using SharpRpc.Reflection;
using System.Linq;

namespace SharpRpc.ClientSide
{
    public class ServiceProxyClassFactory : IServiceProxyClassFactory
    {
        private const int MaxInlinableComplexity = 16;

        private readonly IServiceDescriptionBuilder serviceDescriptionBuilder;
        private readonly ICodecContainer codecContainer;
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;
        private int classNameDisambiguator = 0;

        public ServiceProxyClassFactory(IServiceDescriptionBuilder serviceDescriptionBuilder, ICodecContainer codecContainer)
        {
            this.serviceDescriptionBuilder = serviceDescriptionBuilder;
            this.codecContainer = codecContainer;
            var appDomain = AppDomain.CurrentDomain;
            assemblyBuilder = appDomain.DefineDynamicAssembly(new AssemblyName("SharpRpcServiceProxies"), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("SharpRpcServiceProxyModule");
        }

        private static readonly Type[] ConstructorParameterTypes = new[] { typeof(IOutgoingMethodCallProcessor), typeof(string), typeof(TimeoutSettings), typeof(ICodecContainer) };
        private static readonly MethodInfo GetManualCodecForMethod = typeof(ICodecContainer).GetMethod("GetManualCodecFor");
        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");
        private static readonly MethodInfo ProcessMethod = typeof(IOutgoingMethodCallProcessor).GetMethod("Process");

        public Func<IOutgoingMethodCallProcessor, string, TimeoutSettings, T> CreateProxyClass<T>()
        {
            var proxyClass = CreateProxyClass(typeof(T), typeof(T), null);
            return (p, s, t) => (T) Activator.CreateInstance(proxyClass, p, s, t, codecContainer);
        }

        private Type CreateProxyClass(Type rootType, Type type, string path)
        {
            var serviceDescription = serviceDescriptionBuilder.Build(type);
            path = path ?? serviceDescription.Name;

            var typeBuilder = DeclareType(type);
            var fields = DeclareFields(typeBuilder);
            var manualCodecTypes = new List<Type>();
            foreach (var methodDesc in serviceDescription.Methods)
                CreateMethod(rootType, path, methodDesc, typeBuilder, fields, manualCodecTypes);
            CreateConstructor(rootType, path, serviceDescription, typeBuilder, fields, manualCodecTypes);
            return typeBuilder.CreateType();
        }

        private TypeBuilder DeclareType(Type serviceInterface)
        {
            int disambiguator = Interlocked.Increment(ref classNameDisambiguator);
            return moduleBuilder.DefineType("__rpc_proxy_" + serviceInterface.FullName + "_" + disambiguator,
                                            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                                            typeof(object), new[] { serviceInterface });
        }

        private static ServiceProxyFields DeclareFields(TypeBuilder typeBuilder)
        {
            return new ServiceProxyFields
            {
                Processor         = typeBuilder.DefineField("methodCallProcessor", typeof(IOutgoingMethodCallProcessor), FieldAttributes.Private | FieldAttributes.InitOnly),
                Scope             = typeBuilder.DefineField("scope",               typeof(string),                       FieldAttributes.Private | FieldAttributes.InitOnly),
                TimeoutSettings   = typeBuilder.DefineField("timeoutSettings",     typeof(TimeoutSettings),              FieldAttributes.Private | FieldAttributes.InitOnly),
                CodecContainer    = typeBuilder.DefineField("codecContainer",      typeof(ICodecContainer),              FieldAttributes.Private | FieldAttributes.InitOnly),
                ManualCodecs      = typeBuilder.DefineField("manualCodecs",        typeof(IManualCodec[]),               FieldAttributes.Private | FieldAttributes.InitOnly)
            };
        }

        private void CreateMethod(Type rootType, string path, MethodDescription methodDesc, TypeBuilder typeBuilder, ServiceProxyFields fields, List<Type> manualCodecTypes)
        {
            var methodBuilder = typeBuilder.DefineMethod(methodDesc.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual);

            var genericTypeParameterBuilders = methodDesc.GenericParameters.Any()
                ? methodBuilder.DefineGenericParameters(methodDesc.GenericParameters.Select(x => x.Name).ToArray())
                : new GenericTypeParameterBuilder[0];
            var genericTypeParameters = genericTypeParameterBuilders.Select(x => new ServiceProxyMethodGenericTypeParameterNecessity(codecContainer, x)).ToArray();
            var genericArgumentMap = genericTypeParameterBuilders.ToDictionary(x => x.Name, x => (Type)x);
            var parameters = methodDesc.Parameters.Select((x, i) => new ServiceProxyMethodParameterNecessity(codecContainer, x, genericArgumentMap)).ToArray();

            var retvalType = methodDesc.ReturnType.DeepSubstituteGenerics(genericArgumentMap);
            var parameterTypesAdjustedForRefs = parameters.Select((x, i) => methodDesc.Parameters[i].Way == MethodParameterWay.Val ? x.ConcreteType : x.ConcreteType.MakeByRefType()).ToArray();

            methodBuilder.SetParameters(parameterTypesAdjustedForRefs);
            methodBuilder.SetReturnType(retvalType);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

            var requestParameters = parameters
                .Where(x => x.Description.Way == MethodParameterWay.Val || x.Description.Way == MethodParameterWay.Ref)
                .ToArray();

            var responseParameters = parameters
                .Where(x => x.Description.Way == MethodParameterWay.Ref || x.Description.Way == MethodParameterWay.Out)
                .ToArray();

            bool hasRetval = methodDesc.ReturnType != typeof(void);

            var il = methodBuilder.GetILGenerator();
            var emittingContext = new EmittingContext(il, responseParameters.Any() || hasRetval);

            var typeCodec = codecContainer.GetEmittingCodecFor(typeof(Type));
            var requestDataArrayVar = il.DeclareLocal(typeof(byte[]));        // byte[] dataArray
            if (requestParameters.Any() || genericTypeParameters.Any())
            {
                bool haveSizeOnStack = false;
                foreach (var typeParameter in genericTypeParameters)
                {
                    typeCodec.EmitCalculateSize(emittingContext, typeParameter.EmitLoad);
                    EmitAddIf(il, ref haveSizeOnStack);
                }
                foreach (var parameter in requestParameters)
                {
                    EmitCalculateSize(emittingContext, manualCodecTypes, fields, parameter.Codec, parameter.ConcreteType, parameter.EmitLoad);
                    EmitAddIf(il, ref haveSizeOnStack);
                }

                il.Emit(OpCodes.Newarr, typeof(byte));                      // dataArray = new byte[stack_0]
                il.Emit(OpCodes.Stloc, requestDataArrayVar);
                var pinnedVar =                                             // var pinned dataPointer = pin(dataArray)
                    il.Emit_PinArray(typeof(byte), requestDataArrayVar);
                il.Emit(OpCodes.Ldloc, pinnedVar);                          // data = dataPointer
                il.Emit(OpCodes.Stloc, emittingContext.DataPointerVar);

                foreach (var typeParameter in genericTypeParameters)
                    typeCodec.EmitEncode(emittingContext, typeParameter.EmitLoad);
                foreach (var parameter in requestParameters)
                    EmitEncode(emittingContext, manualCodecTypes, fields, parameter.Codec, parameter.ConcreteType, parameter.EmitLoad);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);                                // dataArray = null
                il.Emit(OpCodes.Stloc, requestDataArrayVar);
            }

            il.Emit_Ldarg(0);                                           // stack_0 = methodCallProcessor
            il.Emit(OpCodes.Ldfld, fields.Processor);
            il.Emit(OpCodes.Ldtoken, rootType);                         // stack_1 = typeof(T)
            il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            il.Emit(OpCodes.Ldstr, string.Format("{0}/{1}",             // stack_2 = SuperServicePath/ServiceName/MethodName
                path, methodDesc.Name));
            il.Emit_Ldarg(0);                                           // stack_3 = scope
            il.Emit(OpCodes.Ldfld, fields.Scope);
            il.Emit(OpCodes.Ldloc, requestDataArrayVar);                // stack_4 = dataArray
            il.Emit_Ldarg(0);                                           // stack_5 = timeoutSettings
            il.Emit(OpCodes.Ldfld, fields.TimeoutSettings);
            il.Emit(OpCodes.Callvirt, ProcessMethod);                   // stack_0 = stack_0.Process(stack_1, stack_2, stack_3, stack_4, stack_5)

            if (responseParameters.Any() || hasRetval)
            {
                var responseDataArrayVar = il.DeclareLocal(typeof(byte[]));
                il.Emit(OpCodes.Stloc, responseDataArrayVar);           // dataArray = stack_0
                il.Emit(OpCodes.Ldloc, responseDataArrayVar);           // remainingBytes = dataArray.Length
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Stloc, emittingContext.RemainingBytesVar);
                var pinnedVar =                                         // var pinned dataPointer = pin(dataArray)
                    il.Emit_PinArray(typeof(byte), responseDataArrayVar);
                il.Emit(OpCodes.Ldloc, pinnedVar);                      // data = dataPointer
                il.Emit(OpCodes.Stloc, emittingContext.DataPointerVar);

                foreach (var parameter in responseParameters)
                {
                    il.Emit(OpCodes.Ldarg, parameter.Description.Index + 1);// arg_i+1 = decode(data, remainingBytes, false)
                    EmitDecode(emittingContext, manualCodecTypes, fields, parameter.Codec, parameter.ConcreteType);
                    il.Emit_Stind(parameter.ConcreteType);
                }

                if (hasRetval)
                {
                    var retvalCodec = methodDesc.ReturnType == retvalType ? codecContainer.GetEmittingCodecFor(retvalType) : null;
                    EmitDecode(emittingContext, manualCodecTypes, fields, retvalCodec, retvalType);
                }
            }
            else
            {
                il.Emit(OpCodes.Pop);                               // pop(stack_0)
            }

            il.Emit(OpCodes.Ret);
        }

        private static void EmitAddIf(ILGenerator il, ref bool haveSizeOnStack)
        {
            if (haveSizeOnStack)
                il.Emit(OpCodes.Add);
            else
                haveSizeOnStack = true;
        }

        private static void EmitCalculateSize(IEmittingContext emittingContext, List<Type> manualCodecTypes, ServiceProxyFields fields, IEmittingCodec emittingCodec, Type concreteType, Action<ILGenerator> emitLoad)
        {
            var il = emittingContext.IL;
            if (concreteType.ContainsGenericParameters)
            {
                il.Emit_Ldarg(0);
                il.Emit(OpCodes.Ldfld, fields.CodecContainer);
                il.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(concreteType));
                emitLoad(il);
                var methodInfo = typeof(IManualCodec<>).GetMethod("CalculateSize");
                var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(concreteType);
                var genericMethodInfo = TypeBuilder.GetMethod(concreteCodecType, methodInfo);
                il.Emit(OpCodes.Callvirt, genericMethodInfo);
            }
            else if (emittingCodec.CanBeInlined && emittingCodec.EncodingComplexity <= MaxInlinableComplexity)
            {
                emittingCodec.EmitCalculateSize(emittingContext, emitLoad);
            }
            else
            {
                int indexOfCodec = manualCodecTypes.IndexOfFirst(x => x == concreteType);
                if (indexOfCodec == -1)
                {
                    indexOfCodec = manualCodecTypes.Count;
                    manualCodecTypes.Add(concreteType);
                }
                il.Emit_Ldarg(0);
                il.Emit(OpCodes.Ldfld, fields.ManualCodecs);
                il.Emit_Ldc_I4(indexOfCodec);
                il.Emit(OpCodes.Ldelem_Ref);
                var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(concreteType);
                il.Emit(OpCodes.Isinst, concreteCodecType);
                emitLoad(il);
                il.Emit(OpCodes.Callvirt, concreteCodecType.GetMethod("CalculateSize"));
            }
        }

        private static void EmitEncode(IEmittingContext emittingContext, List<Type> manualCodecTypes, ServiceProxyFields fields, IEmittingCodec emittingCodec, Type concreteType, Action<ILGenerator> emitLoad)
        {
            var il = emittingContext.IL;
            if (concreteType.ContainsGenericParameters)
            {
                il.Emit_Ldarg(0);
                il.Emit(OpCodes.Ldfld, fields.CodecContainer);
                il.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(concreteType));
                il.Emit(OpCodes.Ldloca, emittingContext.DataPointerVar);
                emitLoad(il);
                var methodInfo = typeof(IManualCodec<>).GetMethod("Encode");
                var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(concreteType);
                var genericMethodInfo = TypeBuilder.GetMethod(concreteCodecType, methodInfo);
                il.Emit(OpCodes.Callvirt, genericMethodInfo);
            }
            else if (emittingCodec.CanBeInlined && emittingCodec.EncodingComplexity <= MaxInlinableComplexity)
            {
                emittingCodec.EmitEncode(emittingContext, emitLoad);
            }
            else
            {
                int indexOfCodec = manualCodecTypes.IndexOfFirst(x => x == concreteType);
                if (indexOfCodec == -1)
                {
                    indexOfCodec = manualCodecTypes.Count;
                    manualCodecTypes.Add(concreteType);
                }
                il.Emit_Ldarg(0);
                il.Emit(OpCodes.Ldfld, fields.ManualCodecs);
                il.Emit_Ldc_I4(indexOfCodec);
                il.Emit(OpCodes.Ldelem_Ref);
                var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(concreteType);
                il.Emit(OpCodes.Isinst, concreteCodecType);
                il.Emit(OpCodes.Ldloca, emittingContext.DataPointerVar);
                emitLoad(il);
                il.Emit(OpCodes.Callvirt, concreteCodecType.GetMethod("Encode"));
            }
        }

        private static void EmitDecode(IEmittingContext emittingContext, List<Type> manualCodecTypes, ServiceProxyFields fields, IEmittingCodec emittingCodec, Type concreteType)
        {
            var il = emittingContext.IL;
            if (concreteType.ContainsGenericParameters)
            {
                il.Emit_Ldarg(0);
                il.Emit(OpCodes.Ldfld, fields.CodecContainer);
                il.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(concreteType));
                il.Emit(OpCodes.Ldloca, emittingContext.DataPointerVar);
                il.Emit(OpCodes.Ldloca, emittingContext.RemainingBytesVar);
                il.Emit_Ldc_I4(0);
                var methodInfo = typeof(IManualCodec<>).GetMethod("Decode");
                var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(concreteType);
                var genericMethodInfo = TypeBuilder.GetMethod(concreteCodecType, methodInfo);
                il.Emit(OpCodes.Callvirt, genericMethodInfo);
            }
            else if (emittingCodec.CanBeInlined && emittingCodec.EncodingComplexity <= MaxInlinableComplexity)
            {
                emittingCodec.EmitDecode(emittingContext, false);
            }
            else
            {
                int indexOfCodec = manualCodecTypes.IndexOfFirst(x => x == concreteType);
                if (indexOfCodec == -1)
                {
                    indexOfCodec = manualCodecTypes.Count;
                    manualCodecTypes.Add(concreteType);
                }
                il.Emit_Ldarg(0);
                il.Emit(OpCodes.Ldfld, fields.ManualCodecs);
                il.Emit_Ldc_I4(indexOfCodec);
                il.Emit(OpCodes.Ldelem_Ref);
                var concreteCodecType = typeof(IManualCodec<>).MakeGenericType(concreteType);
                il.Emit(OpCodes.Isinst, concreteCodecType);
                il.Emit(OpCodes.Ldloca, emittingContext.DataPointerVar);
                il.Emit(OpCodes.Ldloca, emittingContext.RemainingBytesVar);
                il.Emit_Ldc_I4(0);
                il.Emit(OpCodes.Callvirt, concreteCodecType.GetMethod("Decode"));
            }
        }

        private void CreateConstructor(Type rootType, string path, ServiceDescription serviceDescription, TypeBuilder typeBuilder, ServiceProxyFields fields, List<Type> manualCodecTypes)
        {
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
            cil.Emit(OpCodes.Stfld, fields.Processor);
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldarg_2);
            cil.Emit(OpCodes.Stfld, fields.Scope);
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Ldarg_3);
            cil.Emit(OpCodes.Stfld, fields.TimeoutSettings);
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit_Ldarg(4);
            cil.Emit(OpCodes.Stfld, fields.CodecContainer);
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit_Ldc_I4(manualCodecTypes.Count);
            cil.Emit(OpCodes.Newarr, typeof(IManualCodec));
            cil.Emit(OpCodes.Stfld, fields.ManualCodecs);
            for (int i = 0; i < manualCodecTypes.Count; i++)
            {
                var codecType = manualCodecTypes[i];
                cil.Emit_Ldarg(0);
                cil.Emit(OpCodes.Ldfld, fields.ManualCodecs);
                cil.Emit_Ldc_I4(i);
                cil.Emit_Ldarg(4);
                cil.Emit(OpCodes.Call, GetManualCodecForMethod.MakeGenericMethod(codecType));
                cil.Emit(OpCodes.Stelem_Ref);
            }
            #endregion

            foreach (var subserviceDesc in serviceDescription.Subservices)
            {
                #region Emit Subservice Property
                var proxyClass = CreateProxyClass(rootType, subserviceDesc.Service.Type, path + "/" + subserviceDesc.Name);

                var fieldBuilder = typeBuilder.DefineField("_" + subserviceDesc.Name, proxyClass,
                    FieldAttributes.Private | FieldAttributes.InitOnly);

                cil.Emit_Ldarg(0);
                cil.Emit_Ldarg(1);
                cil.Emit_Ldarg(2);
                cil.Emit_Ldarg(3);
                cil.Emit_Ldarg(4);
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
        }
    }
}

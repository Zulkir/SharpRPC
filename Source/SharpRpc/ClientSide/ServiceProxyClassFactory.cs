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
using System.Reflection;
using System.Reflection.Emit;
using SharpRpc.Codecs;
using SharpRpc.Reflection;
using System.Linq;

namespace SharpRpc.ClientSide
{
    public class ServiceProxyClassFactory : IServiceProxyClassFactory
    {
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

            foreach (var methodDescription in serviceDescription.Methods)
            {
                #region Emit Method
                var methodBuilder = typeBuilder.DefineMethod(methodDescription.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    methodDescription.ReturnType, methodDescription.Parameters.Select(x => x.Type).ToArray());
                methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

                var hasArgs = methodDescription.Parameters.Count > 0;
                var hasRetval = methodDescription.ReturnType != typeof(void);

                var il = methodBuilder.GetILGenerator();
                var locals = new LocalVariableCollection(il, hasRetval);
                var dataArrayVar =                                      // byte[] dataArray
                    locals.GetOrAdd("dataArray",
                        lil => lil.DeclareLocal(typeof(byte[])));

                if (hasArgs)
                {
                    var codecs = methodDescription.Parameters
                        .Select(x => codecContainer.GetEmittingCodecFor(x.Type)).ToArray();

                    il.Emit_LoadSize(codecs[0], 1);                          // stack_0 = size of arg_1
                    for (int i = 1; i < codecs.Length; i++)
                    {
                        il.Emit_LoadSize(codecs[i], i + 1);                  // stack_1 = size of arg_i+1
                        il.Emit(OpCodes.Add);                           // stack_0 = stack_1 + stack_0
                    }

                    il.Emit(OpCodes.Newarr, typeof(byte));              // dataArray = new byte[stack_0]
                    il.Emit(OpCodes.Stloc, dataArrayVar);
                    var dataPointerVar =                                // var pinned dataPointer = pin(dataArray)
                        il.Emit_PinArray(typeof(byte), locals, dataArrayVar);
                    il.Emit(OpCodes.Ldloc, dataPointerVar);             // data = dataPointer
                    il.Emit(OpCodes.Stloc, locals.DataPointer);
                    
                    for (int i = 0; i < codecs.Length; i++)
                        codecs[i].EmitEncode(il, locals, i + 1);        // encode(data, arg_i+1)

                    il.Emit_UnpinArray(dataPointerVar);                  // unpin(dataPointer)
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);                            // dataArray = null
                    il.Emit(OpCodes.Stloc, dataArrayVar);
                }

                il.Emit(OpCodes.Ldarg_0);                               // stack_0 = this.methodCallProcessor
                il.Emit(OpCodes.Ldfld, processorField);
                il.Emit(OpCodes.Ldtoken, type);                         // stack_1 = typeof(T)
                il.Emit(OpCodes.Call, GetTypeFromHandleMethod);
                il.Emit(OpCodes.Ldstr, string.Format("{0}/{1}",         // stack_2 = SuperServicePath/ServiceName/MethodName
                    path, methodDescription.Name));
                il.Emit(OpCodes.Ldarg_0);                               // stack_3 = this.scope
                il.Emit(OpCodes.Ldfld, scopeField);
                il.Emit(OpCodes.Ldloc, dataArrayVar);                   // stack_4 = dataArray
                il.Emit(OpCodes.Callvirt, ProcessMethod);               // stack_0 = stack_0.Process(stack_1, stack_2, stack_3, stack_4)

                if (hasRetval)
                {
                    var retvalCodec = codecContainer.GetEmittingCodecFor(methodDescription.ReturnType);

                    il.Emit(OpCodes.Stloc, dataArrayVar);               // dataArray = stack_0
                    il.Emit(OpCodes.Ldloc, dataArrayVar);               // remainingBytes = dataArray.Length
                    il.Emit(OpCodes.Ldlen);
                    il.Emit(OpCodes.Stloc, locals.RemainingBytes);
                    var dataPointerVar =                                // var pinned dataPointer = pin(dataArray)
                        il.Emit_PinArray(typeof(byte), locals, dataArrayVar);
                    il.Emit(OpCodes.Ldloc, dataPointerVar);             // data = dataPointer
                    il.Emit(OpCodes.Stloc, locals.DataPointer);
                    retvalCodec.EmitDecode(il, locals, false);          // stack_0 = decode(data, remainingBytes, false)
                    il.Emit_UnpinArray(dataPointerVar);                  // unpin(dataPointer)
                }
                else
                {
                    il.Emit(OpCodes.Pop);                               // pop(stack_0)
                }
                il.Emit(OpCodes.Ret);
                #endregion
            }

            return typeBuilder.CreateType();
        }
    }
}

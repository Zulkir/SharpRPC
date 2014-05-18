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
using System.Threading.Tasks;
using SharpRpc.Codecs;
using SharpRpc.Reflection;
using System.Linq;
using SharpRpc.Utility;

namespace SharpRpc.ClientSide.Proxy
{
    public class ProxyFactory : IProxyFactory
    {
        private readonly IServiceDescriptionBuilder serviceDescriptionBuilder;
        private readonly ICodecContainer codecContainer;
        private readonly ModuleBuilder moduleBuilder;
        private int classNameDisambiguator;

        public ProxyFactory(IServiceDescriptionBuilder serviceDescriptionBuilder, ICodecContainer codecContainer)
        {
            this.serviceDescriptionBuilder = serviceDescriptionBuilder;
            this.codecContainer = codecContainer;
            var appDomain = AppDomain.CurrentDomain;
            var assemblyBuilder = appDomain.DefineDynamicAssembly(new AssemblyName("SharpRpcServiceProxies"), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("SharpRpcServiceProxyModule");
        }

        private static readonly Type[] ConstructorParameterTypes = { typeof(IOutgoingRequestProcessor), typeof(string), typeof(TimeoutSettings), typeof(ICodecContainer) };
        private static readonly Type[] DecodeDeferredParameterTypes = { typeof(Task<byte[]>) };

        public Func<IOutgoingRequestProcessor, string, TimeoutSettings, T> CreateProxy<T>()
        {
            var proxyClass = CreateProxyClass(typeof(T), typeof(T), null);
            return (p, s, t) => (T) Activator.CreateInstance(proxyClass, p, s, t, codecContainer);
        }

        private Type CreateProxyClass(Type rootInterfaceType, Type interfaceType, string path)
        {
            var serviceDescription = serviceDescriptionBuilder.Build(interfaceType);
            path = path ?? serviceDescription.Name;

            var typeBuilder = DeclareType(interfaceType);
            var fieldCache = new ProxyClassFieldCache(typeBuilder);
            var classContext = new ProxyClassBuildingContext(rootInterfaceType, path, typeBuilder, fieldCache);
            foreach (var methodDesc in serviceDescription.Methods)
                CreateMethod(classContext, path, methodDesc);
            CreateConstructor(classContext, path, serviceDescription);
            return typeBuilder.CreateType();
        }

        private TypeBuilder DeclareType(Type serviceInterface)
        {
            int disambiguator = Interlocked.Increment(ref classNameDisambiguator);
            return moduleBuilder.DefineType("__rpc_proxy_" + serviceInterface.FullName + "_" + disambiguator,
                                            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                                            typeof(object), new[] { serviceInterface });
        }

        private void CreateMethod(ProxyClassBuildingContext classContext, string path, MethodDescription methodDesc)
        {
            var methodBuilder = classContext.Builder.DefineMethod(methodDesc.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual);

            var genericTypeParameterBuilders = methodDesc.GenericParameters.Any()
                ? methodBuilder.DefineGenericParameters(methodDesc.GenericParameters.Select(x => x.Name).ToArray())
                : new GenericTypeParameterBuilder[0];
            var genericTypeParameterCodecs = genericTypeParameterBuilders.Select(x => new ProxyMethodGenericArgumentCodec(x)).ToArray();
            var genericArgumentMap = genericTypeParameterBuilders.ToDictionary(x => x.Name, x => (Type)x);

            var parameterDescriptions = methodDesc.Parameters.Select(x => x.DeepSubstituteGenerics(genericArgumentMap)).ToArray();
            var parameterTypesAdjustedForRefs = parameterDescriptions.Select(x => x.Way == MethodParameterWay.Val ? x.Type : x.Type.MakeByRefType()).ToArray();
            var allParameterCodecs = parameterDescriptions.Select(x => new ProxyMethodParameterCodec(x)).ToArray();

            var retvalType = methodDesc.ReturnType.DeepSubstituteGenerics(genericArgumentMap);
            
            methodBuilder.SetParameters(parameterTypesAdjustedForRefs);
            methodBuilder.SetReturnType(retvalType);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

            var il = new MyILGenerator(methodBuilder.GetILGenerator());
            var emittingContext = new ProxyMethodEmittingContext(il, classContext.Fields);

            var requestDataArrayVar = il.DeclareLocal(typeof(byte[]));                      // byte[] dataArray

            var requestParametersCodecs = allParameterCodecs.Where(x => x.IsRequestParameter).ToArray();

            if (requestParametersCodecs.Any() || genericTypeParameterBuilders.Any())
            {
                bool haveSizeOnStack = false;
                foreach (var codec in genericTypeParameterCodecs)
                {
                    codec.EmitCalculateSize(emittingContext);                               // stack_0 += CalculateSize(T_i)
                    EmitAddIf(il, ref haveSizeOnStack);
                }
                foreach (var parameter in requestParametersCodecs)
                {
                    parameter.EmitCalculateSize(emittingContext);                           // stack_0 += CalculateSize(arg_i)    
                    EmitAddIf(il, ref haveSizeOnStack);
                }

                il.Newarr(typeof(byte));                                                    // dataArray = new byte[stack_0]
                il.Stloc(requestDataArrayVar);
                var pinnedVar = il.PinArray(typeof(byte), requestDataArrayVar);             // var pinned dataPointer = pin(dataArray)
                il.Ldloc(pinnedVar);                                                        // data = dataPointer
                il.Stloc(emittingContext.DataPointerVar);

                foreach (var codec in genericTypeParameterCodecs)
                    codec.EmitEncode(emittingContext);                                      // Encode(T_i, data)
                foreach (var codec in requestParametersCodecs)
                    codec.EmitEncode(emittingContext);                                      // Encode(arg_i, data)
            }
            else
            {
                il.Ldnull();                                                                // dataArray = null
                il.Stloc(requestDataArrayVar);
            }

            il.Ldarg(0);                                                                    // stack_0 = methodCallProcessor
            il.Ldfld(classContext.Fields.Processor);
            il.Ldtoken(classContext.InterfaceType);                                         // stack_1 = typeof(T)
            il.Call(TypeMethods.GetTypeFromHandle);
            il.Ldstr(string.Format("{0}/{1}", path, methodDesc.Name));                      // stack_2 = SuperServicePath/ServiceName/MethodName
            il.Ldarg(0);                                                                    // stack_3 = scope
            il.Ldfld(classContext.Fields.Scope);
            il.Ldloc(requestDataArrayVar);                                                  // stack_4 = dataArray
            il.Ldarg(0);                                                                    // stack_5 = timeoutSettings
            il.Ldfld(classContext.Fields.TimeoutSettings);

            var responseParameterCodecs = allParameterCodecs.Where(x => x.IsResponseParameter).ToArray();

            if (responseParameterCodecs.Any() && methodDesc.RemotingType != MethodRemotingType.Direct)
                throw new ArgumentException(string.Format("Error processing {0} method: only direct methods can have Ref or Out parameters", path));

            switch (methodDesc.RemotingType)
            {
                case MethodRemotingType.Direct:
                    EmitProcessDirect(emittingContext, responseParameterCodecs, retvalType);
                    break;
                case MethodRemotingType.AsyncVoid:
                    EmitProcessAsyncVoid(il);
                    break;
                case MethodRemotingType.AsyncWithRetval:
                    EmitProcessAsyncWithRetval(classContext, emittingContext, methodDesc.Name, genericTypeParameterBuilders, retvalType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            il.Ret();
        }

        private static void EmitAddIf(MyILGenerator il, ref bool haveSizeOnStack)
        {
            if (haveSizeOnStack)
                il.Add();
            else
                haveSizeOnStack = true;
        }

        private void EmitProcessDirect(IEmittingContext emittingContext, ProxyMethodParameterCodec[] responseParameterCodecs, Type retvalType)
        {
            bool hasRetval = retvalType != typeof(void);
            var il = emittingContext.IL;
            il.Callvirt(OutgoingRequestProcessorMethods.Process);                // stack_0 = stack_0.Process(stack_1, stack_2, stack_3, stack_4, stack_5)

            if (responseParameterCodecs.Any() || hasRetval)
            {
                EmitPrepareToDecode(emittingContext);

                foreach (var codec in responseParameterCodecs)
                    codec.EmitDecodeAndStore(emittingContext);                      // arg_i = Decode(data, remainingBytes, false)

                if (hasRetval)
                {
                    var retvalCodec = new ProxyMethodRetvalCodec(retvalType);
                    retvalCodec.EmitDecode(emittingContext);                        // stack_0 = Decode(data, remainingBytes, false)
                }
            }
            else
            {
                il.Pop();                                                           // pop(stack_0)
            }
        }

        private static void EmitProcessAsyncVoid(MyILGenerator il)
        {
            il.Callvirt(OutgoingRequestProcessorMethods.ProcessAsync);           // stack_0 = stack_0.ProcessAsync(stack_1, stack_2, stack_3, stack_4, stack_5)
        }

        private void EmitProcessAsyncWithRetval(ProxyClassBuildingContext classContext, IEmittingContext emittingContext, string parameterMethodName, IReadOnlyList<Type> genericTypeArguments, Type retvalType)
        {
            var pureRetvalType = retvalType.GetGenericArguments().Single();
            var decodeDeferredMethod = CreateDecodeDeferredMethod(classContext, parameterMethodName, genericTypeArguments, pureRetvalType);
            if (decodeDeferredMethod.IsGenericMethodDefinition)
                decodeDeferredMethod = decodeDeferredMethod.MakeGenericMethod(genericTypeArguments.ToArray());
            var funcConstructor = FuncMethods.Constructor(typeof(Task<byte[]>), pureRetvalType);
            var continueWithMethod = TaskMethods.ContinueWith(typeof(byte[]), pureRetvalType);

            var il = emittingContext.IL;
            il.Callvirt(OutgoingRequestProcessorMethods.ProcessAsync);
            il.Ldarg(0);
            il.Ldftn(decodeDeferredMethod);
            il.Newobj(funcConstructor);
            il.Callvirt(continueWithMethod);
        }

        private MethodInfo CreateDecodeDeferredMethod(ProxyClassBuildingContext classContext, string parentMethodName, IReadOnlyList<Type> genericTypeArguments, Type pureRetvalType)
        {
            var methodBuilder = classContext.Builder.DefineMethod("__rpc_decode_deferred_" + parentMethodName,
                    MethodAttributes.Private | MethodAttributes.HideBySig);

            var genericTypeParameterBuilders = genericTypeArguments.Any()
                ? methodBuilder.DefineGenericParameters(genericTypeArguments.Select(x => x.Name).ToArray())
                : new GenericTypeParameterBuilder[0];
            var genericArgumentMap = genericTypeParameterBuilders.ToDictionary(x => x.Name, x => (Type)x);

            var retvalType = pureRetvalType.DeepSubstituteGenerics(genericArgumentMap);

            methodBuilder.SetParameters(DecodeDeferredParameterTypes);
            methodBuilder.SetReturnType(retvalType);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);

            var il = new MyILGenerator(methodBuilder.GetILGenerator());
            var emittingContext = new ProxyMethodEmittingContext(il, classContext.Fields);

            il.Ldarg(1);
            il.Call(TaskMethods.GetResult(typeof(byte[])));
            EmitPrepareToDecode(emittingContext);

            var retvalCodec = new ProxyMethodRetvalCodec(retvalType);
            retvalCodec.EmitDecode(emittingContext);
            il.Ret();

            return methodBuilder;
        }

        private static void EmitPrepareToDecode(IEmittingContext emittingContext)
        {
            var il = emittingContext.IL;
            var responseDataArrayVar = il.DeclareLocal(typeof(byte[]));
            il.Stloc(responseDataArrayVar);                                     // dataArray = stack_0
            il.Ldloc(responseDataArrayVar);                                     // remainingBytes = dataArray.Length
            il.Ldlen();
            il.Stloc(emittingContext.RemainingBytesVar);
            var pinnedVar = il.PinArray(typeof(byte), responseDataArrayVar);    // var pinned dataPointer = pin(dataArray)
            il.Ldloc(pinnedVar);                                                // data = dataPointer
            il.Stloc(emittingContext.DataPointerVar);
        }

        private void CreateConstructor(ProxyClassBuildingContext classContext, string path, ServiceDescription serviceDescription)
        {
            const int thisArgIndex = 0;
            const int processorArgIndex = 1;
            const int scopeArgIndex = 2;
            const int timeoutSettingsArgIndex = 3;
            const int codecContainerArgIndex = 4;

            var constructorBuilder = classContext.Builder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard, ConstructorParameterTypes);
            var baseConstructor = typeof(object).GetConstructor(Type.EmptyTypes);
            var il = new MyILGenerator(constructorBuilder.GetILGenerator());
            il.Ldarg(thisArgIndex);
            il.Call(baseConstructor);
            il.Ldarg(thisArgIndex);
            il.Ldarg(processorArgIndex);
            il.Stfld(classContext.Fields.Processor);
            il.Ldarg(thisArgIndex);
            il.Ldarg(scopeArgIndex);
            il.Stfld(classContext.Fields.Scope);
            il.Ldarg(thisArgIndex);
            il.Ldarg(timeoutSettingsArgIndex);
            il.Stfld(classContext.Fields.TimeoutSettings);
            il.Ldarg(thisArgIndex);
            il.Ldarg(codecContainerArgIndex);
            il.Stfld(classContext.Fields.CodecContainer);

            foreach (var manualCodecField in classContext.Fields.GetAllManualCodecFields())
            {
                var type = manualCodecField.FieldType.GenericTypeArguments[0];
                ValidateManualCodecType(type);

                il.Ldarg(thisArgIndex);
                il.Ldarg(codecContainerArgIndex);
                il.Call(CodecContainerMethods.GetManualCodecFor(type));
                il.Stfld(manualCodecField);
            }

            foreach (var subserviceDesc in serviceDescription.Subservices)
            {
                var proxyClass = CreateProxyClass(classContext.InterfaceType, subserviceDesc.Type, path + "/" + subserviceDesc.Name);
                var fieldBuilder = CreateSubserviceField(classContext, subserviceDesc, proxyClass);
                CreateSubserviceProperty(classContext, subserviceDesc, fieldBuilder);

                il.Ldarg(thisArgIndex);
                il.Ldarg(processorArgIndex);
                il.Ldarg(scopeArgIndex);
                il.Ldarg(timeoutSettingsArgIndex);
                il.Ldarg(codecContainerArgIndex);
                il.Newobj(proxyClass.GetConstructor(ConstructorParameterTypes));
                il.Stfld(fieldBuilder);
            }

            il.Ret();
        }

        private void ValidateManualCodecType(Type type)
        {
            codecContainer.GetEmittingCodecFor(type);
        }

        private static FieldBuilder CreateSubserviceField(ProxyClassBuildingContext classContext, ServiceDescription subserviceDescription, Type proxyClass)
        {
            return classContext.Builder.DefineField("_" + subserviceDescription.Name, proxyClass,
                    FieldAttributes.Private | FieldAttributes.InitOnly);
        }

        private static void CreateSubserviceProperty(ProxyClassBuildingContext classContext, ServiceDescription subserviceDescription, FieldBuilder fieldBuilder)
        {
            var methodBuilder = classContext.Builder.DefineMethod("get_" + subserviceDescription.Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    subserviceDescription.Type, Type.EmptyTypes);
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Managed);
            var il = new MyILGenerator(methodBuilder.GetILGenerator());
            il.Ldarg(0);
            il.Ldfld(fieldBuilder);
            il.Ret();

            var propertyBuilder = classContext.Builder.DefineProperty(subserviceDescription.Name,
                PropertyAttributes.None, subserviceDescription.Type, Type.EmptyTypes);
            propertyBuilder.SetGetMethod(methodBuilder);
        }
    }
}

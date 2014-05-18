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
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using System.Linq;
using SharpRpc.Utility;

namespace SharpRpc.ServerSide.Handler
{
    public class RawHandlerFactory : IRawHandlerFactory
    {
        private readonly ICodecContainer codecContainer;
        private readonly ModuleBuilder moduleBuilder;
        private int classNameDisambiguator;

        public RawHandlerFactory(ICodecContainer codecContainer)
        {
            this.codecContainer = codecContainer;
            var appDomain = AppDomain.CurrentDomain;
            var assemblyBuilder = appDomain.DefineDynamicAssembly(new AssemblyName("SharpRpcHandlers"), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("SharpRpcHandlerModule");
        }

        // ReSharper disable UnusedMember.Global
        public static Task<byte[]> ToEmptyByteArrayTask(Task task)
        {
            return task.ContinueWith(t => new byte[0]);
        }
        // ReSharper restore UnusedMember.Global

        private static readonly MethodInfo ToEmptyByteArrayTaskMethod = typeof(RawHandlerFactory).GetMethod("ToEmptyByteArrayTask");

        public Func<Type[], IHandler> CreateGenericClass(IReadOnlyList<ServiceDescription> serviceDescriptionChain, MethodDescription methodDescription, ServicePath path)
        {
            var type = CreateType(serviceDescriptionChain, methodDescription, path);
            if (type.IsGenericType)
                return t => (IHandler)Activator.CreateInstance(type.MakeGenericType(t), codecContainer);
            return t => (IHandler)Activator.CreateInstance(type, codecContainer);
        }

        private Type CreateType(IReadOnlyList<ServiceDescription> serviceDescriptionChain, MethodDescription methodDescription, ServicePath path)
        {
            int disambiguator = Interlocked.Increment(ref classNameDisambiguator);
            var typeBuilder = moduleBuilder.DefineType("__rpc_handler_" + string.Join(".", path) + "_" + disambiguator,
                                            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                                            typeof(object), new[] { typeof(IHandler) });

            var genericTypeParameterBuilders = methodDescription.GenericParameters.Any()
                ? typeBuilder.DefineGenericParameters(methodDescription.GenericParameters.Select(x => x.Name).ToArray())
                : new GenericTypeParameterBuilder[0];

            var fieldCache = new HandlerClassFieldCache(typeBuilder);
            var classContext = new HandlerClassBuildingContext(serviceDescriptionChain, methodDescription, typeBuilder, genericTypeParameterBuilders, fieldCache);

            CreateMethodDelegate(classContext);
            CreateConstructor(classContext);

            return typeBuilder.CreateType();
        }

        private static void CreateMethodDelegate(HandlerClassBuildingContext classContext)
        {
            const int implementationArgIndex = 1;
            const int dataArgIndex = 2;
            const int offsetArgIndex = 3;

            var methodBuilder = classContext.Builder.DefineMethod("Handle",
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual,
                    typeof(Task<byte[]>), new[] { typeof(object), typeof(byte[]), typeof(int) });

            var il = new MyILGenerator(methodBuilder.GetILGenerator());
            var emittingContext = new HandlerMethodEmittingContext(il, classContext.Fields);

            var serviceDescriptionChain = classContext.ServiceDescriptionChain;

            il.Ldarg(implementationArgIndex);                                       // stack_0 = (TServiceImplementation) arg_1
            il.Castclass(serviceDescriptionChain.First().Type);

            for (int i = 0; i < serviceDescriptionChain.Count - 1; i++)
            {
                var current = serviceDescriptionChain[i];
                var next = serviceDescriptionChain[i + 1];
                il.Callvirt(current.Type.GetProperty(next.Name).GetGetMethod());    // stack_0 = stack_0.Property
            }

            var methodDescription = classContext.MethodDescription;

            var genericArgumentMap = methodDescription.GenericParameters
                .Zip(classContext.GenericTypeParameterBuilders, (p, a) => new KeyValuePair<string, Type>(p.Name, a))
                .ToDictionary(x => x.Key, x => x.Value);

            var parameterDescriptions = methodDescription.Parameters.Select(x => x.DeepSubstituteGenerics(genericArgumentMap)).ToArray();
            var retvalType = methodDescription.ReturnType.DeepSubstituteGenerics(genericArgumentMap);

            var allParameterCodecs = parameterDescriptions.Select((x, i) => new HandlerParameterCodec(emittingContext, x)).ToArray();
            var requestParameterCodecs = allParameterCodecs.Where(x => x.IsRequestParameter).ToArray();
            var responseParameterCodecs = allParameterCodecs.Where(x => x.IsResponseParameter).ToArray();

            if (requestParameterCodecs.Any())
            {
                il.Ldarg(dataArgIndex);                                 // remainingBytes = dataArray.Length - offset
                il.Ldlen();
                il.Ldarg(offsetArgIndex);
                il.Sub();
                il.Stloc(emittingContext.RemainingBytesVar);
                var pinnedVar = il.PinArray(typeof(byte), 2);           // var pinned dataPointer = pin(dataArray)
                il.Ldloc(pinnedVar);                                    // data = dataPointer + offset
                il.Ldarg(offsetArgIndex);
                il.Add();
                il.Stloc(emittingContext.DataPointerVar);
            }

            foreach (var codec in allParameterCodecs)
                codec.EmitDecodeAndPrepare();

            // ReSharper disable CoVariantArrayConversion
            var resolvedMethodInfo = classContext.GenericTypeParameterBuilders.Any()
                ? methodDescription.MethodInfo.MakeGenericMethod(classContext.GenericTypeParameterBuilders)
                : methodDescription.MethodInfo;
            il.Callvirt(resolvedMethodInfo);                            // stack_0 = stack_0.Method(stack_1, stack_2, ...)
            // ReSharper restore CoVariantArrayConversion

            switch (methodDescription.RemotingType)
            {
                case MethodRemotingType.Direct:
                    EmitProcessAndEncodeDirect(emittingContext, responseParameterCodecs, retvalType);
                    break;
                case MethodRemotingType.AsyncVoid:
                    EmitProcessAndEncodeAsyncVoid(emittingContext);
                    break;
                case MethodRemotingType.AsyncWithRetval:
                    EmitProcessAndEncodeAsyncWithRetval(classContext, emittingContext, retvalType.GetGenericArguments()[0]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            il.Ret();
        }

        private static void EmitProcessAndEncodeDirect(IEmittingContext emittingContext, HandlerParameterCodec[] responseParameterCodecs, Type retvalType)
        {
            var il = emittingContext.IL;
            EmitEncodeDirect(emittingContext, responseParameterCodecs, retvalType);
            il.Call(TaskMethods.FromResult(typeof(byte[])));
        }

        private static void EmitProcessAndEncodeAsyncVoid(IEmittingContext emittingContext)
        {
            var il = emittingContext.IL;
            il.Call(ToEmptyByteArrayTaskMethod);
        }

        private static void EmitProcessAndEncodeAsyncWithRetval(HandlerClassBuildingContext classContext, IEmittingContext emittingContext, Type pureRetvalType)
        {
            var encodeDeferredMethod = CreateEncodeDeferredMethod(classContext, pureRetvalType);
            var continueWithMethod = TaskMethods.ContinueWith(pureRetvalType, typeof(byte[]));
            
            var il = emittingContext.IL;
            il.Ldarg(0);
            il.Ldftn(encodeDeferredMethod);
            il.Newobj(FuncMethods.Constructor(typeof(Task<>).MakeGenericType(pureRetvalType), typeof(byte[])));
            il.Callvirt(continueWithMethod);
        }

        private static MethodBuilder CreateEncodeDeferredMethod(HandlerClassBuildingContext classContext, Type pureRetvalType)
        {
            const int taskArgIndex = 1;

            var methodBuilder = classContext.Builder.DefineMethod("EncodeDeferred",
                MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(byte[]), new[] {typeof(Task<>).MakeGenericType(pureRetvalType)});

            var il = new MyILGenerator(methodBuilder.GetILGenerator());
            var emittingContext = new HandlerMethodEmittingContext(il, classContext.Fields);

            il.Ldarg(taskArgIndex);
            il.Call(TaskMethods.GetResult(pureRetvalType));
            EmitEncodeDirect(emittingContext, new HandlerParameterCodec[0], pureRetvalType);
            il.Ret();
            return methodBuilder;
        }

        private static void EmitEncodeDirect(IEmittingContext emittingContext, HandlerParameterCodec[] responseParameterCodecs, Type retvalType)
        {
            var il = emittingContext.IL;
            bool hasRetval = retvalType != typeof(void);

            if (hasRetval || responseParameterCodecs.Any())
            {
                HandlerRetvalCodec retvalCodec = null;

                if (hasRetval)
                {
                    retvalCodec = new HandlerRetvalCodec(emittingContext, retvalType);
                    retvalCodec.EmitStore();                                                // var ret = stack_0
                    retvalCodec.EmitCalculateSize();                                        // stack_0 = calculateSize(ret)            
                }

                bool hasSizeOnStack = hasRetval;
                foreach (var codec in responseParameterCodecs)
                {
                    codec.EmitCalculateSize();                                              // stack_0 += calculateSize(param_i)
                    EmitAddIf(il, ref hasSizeOnStack);
                }

                var dataArrayVar = il.DeclareLocal(typeof(byte[]));                         // var dataArray = new byte[size of retval]
                il.Newarr(typeof(byte));
                il.Stloc(dataArrayVar);

                var pinnedVar = il.PinArray(typeof(byte), dataArrayVar);                    // var pinned dataPointer = pin(dataArrayVar)
                il.Ldloc(pinnedVar);                                                        // data = dataPointer
                il.Stloc(emittingContext.DataPointerVar);

                foreach (var codec in responseParameterCodecs)
                    codec.EmitEncode();                                                     // encode(data, param_i)

                if (hasRetval)
                    retvalCodec.EmitEncode();                                               // encode(data, ret)

                il.Ldloc(dataArrayVar);                                                     // stack_0 = dataArray
            }
            else
            {
                il.Ldc_I4(0);                                                               // stack_0 = new byte[0]
                il.Newarr(typeof(byte));
            }
        }

        private static void EmitAddIf(MyILGenerator il, ref bool haveSizeOnStack)
        {
            if (haveSizeOnStack)
                il.Add();
            else
                haveSizeOnStack = true;
        }

        private static void CreateConstructor(HandlerClassBuildingContext classContext)
        {
            const int thisArgIndex = 0;
            const int codecContainerArgIndex = 1;

            var constructorBuilder = classContext.Builder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard, new[] { typeof(ICodecContainer) });
            var baseConstructor = typeof(object).GetConstructor(Type.EmptyTypes);
            var il = new MyILGenerator(constructorBuilder.GetILGenerator());
            il.Ldarg(thisArgIndex);
            il.Call(baseConstructor);
            il.Ldarg(thisArgIndex);
            il.Ldarg(codecContainerArgIndex);
            il.Stfld(classContext.Fields.CodecContainer);

            foreach (var manualCodecField in classContext.Fields.GetAllManualCodecFields())
            {
                il.Ldarg(thisArgIndex);
                il.Ldarg(codecContainerArgIndex);
                il.Call(CodecContainerMethods.GetManualCodecFor(manualCodecField.FieldType.GenericTypeArguments[0]));
                il.Stfld(manualCodecField);
            }

            il.Ret();
        }
    }
}
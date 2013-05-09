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
using SharpRpc.Interaction;
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide
{
    public class ServiceMethodHandlerFactory : IServiceMethodHandlerFactory
    {
        private static readonly Type[] ParameterTypes = new[] {typeof(IServiceImplementation), typeof (byte[])};

        private readonly ICodecContainer codecContainer;

        public ServiceMethodHandlerFactory(ICodecContainer codecContainer)
        {
            this.codecContainer = codecContainer;
        }

        public ServiceMethodHandler CreateMethodHandler(ServiceImplementationInfo serviceImplementationInfo, ServicePath servicePath)
        {
            var serviceInterface = serviceImplementationInfo.Interface;

            var dynamicMethod = new DynamicMethod(
                "__srpc__handle__" + serviceInterface.FullName + "__" + string.Join("_", servicePath),
                typeof(byte[]), ParameterTypes, Assembly.GetExecutingAssembly().ManifestModule);
            var il = dynamicMethod.GetILGenerator();
            var locals = new LocalVariableCollection(il, true);
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, serviceInterface);

            var serviceDesc = serviceImplementationInfo.Description;
            for (int i = 1; i < servicePath.Length - 1; i++)
            {
                var propertyInfo = serviceDesc.Type.GetProperty(servicePath[i]);
                il.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
                SubserviceDescription subserviceDescription;
                if (!serviceDesc.TryGetSubservice(servicePath[i], out subserviceDescription))
                    throw new InvalidPathException();
                serviceDesc = subserviceDescription.Service;
            }

            var methodName = servicePath.MethodName;
            var methodInfo = serviceDesc.Type.GetMethod(methodName);
            MethodDescription methodDesc;
            if (!serviceDesc.TryGetMethod(methodName, out methodDesc))
                throw new InvalidPathException();

            bool hasArgs = methodDesc.Parameters.Count > 0;
            bool hasRetval = methodDesc.ReturnType != typeof(void);

            if (hasArgs)
            {
                il.Emit(OpCodes.Ldarg_1);                       // remainingBytes = arg_1.Length
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Stloc, locals.RemainingBytes);
                var dataPointerVar =                            // var pinned dataPointer = pin(arg_1)
                    il.Emit_PinArray(typeof(byte), locals, 1);
                il.Emit(OpCodes.Ldloc, dataPointerVar);         // data = dataPointer
                il.Emit(OpCodes.Stloc, locals.DataPointer);

                foreach (var parameterDesc in methodDesc.Parameters)
                {
                    var codec = codecContainer.GetEmittingCodecFor(parameterDesc.Type);
                    codec.EmitDecode(il, locals, false);
                }

                il.Emit_UnpinArray(dataPointerVar);              // unpin(dataPointer)
            }
            
            il.Emit(OpCodes.Callvirt, methodInfo);

            if (hasRetval)
            {
                var codec = codecContainer.GetEmittingCodecFor(methodDesc.ReturnType);

                var retvalVar = locals.GetOrAdd("retval",       // var retval = stack_0
                                                lil => lil.DeclareLocal(methodDesc.ReturnType));
                il.Emit(OpCodes.Stloc, retvalVar);

                var dataArrayVar = locals.GetOrAdd("dataArray", // var dataArray = new byte[size of retval]
                                                   lil => lil.DeclareLocal(typeof(byte[])));
                il.Emit_LoadSize(codec, retvalVar);
                il.Emit(OpCodes.Newarr, typeof(byte));
                il.Emit(OpCodes.Stloc, dataArrayVar);
                var dataPointerVar =                            // var pinned dataPointer = pin(dataArrayVar)
                    il.Emit_PinArray(typeof(byte), locals, dataArrayVar);
                il.Emit(OpCodes.Ldloc, dataPointerVar);         // data = dataPointer
                il.Emit(OpCodes.Stloc, locals.DataPointer);

                codec.EmitEncode(il, locals, retvalVar);        // encode(data, retval)

                il.Emit_UnpinArray(dataPointerVar);             // unpin(dataPointer)
                il.Emit(OpCodes.Ldloc, dataArrayVar);           // stack_0 = dataArray
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, 0);                     // stack_0 = new byte[0]
                il.Emit(OpCodes.Newarr, typeof(byte));
            }

            il.Emit(OpCodes.Ret);
            return (ServiceMethodHandler)dynamicMethod.CreateDelegate(typeof(ServiceMethodHandler));
        }
    }
}
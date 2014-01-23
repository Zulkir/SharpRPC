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
using System.Reflection;
using System.Reflection.Emit;
using SharpRpc.Codecs;
using SharpRpc.Interaction;
using SharpRpc.Reflection;
using System.Linq;

namespace SharpRpc.ServerSide
{
    public class ServiceMethodHandlerFactory : IServiceMethodHandlerFactory
    {
        struct ParameterNecessity
        {
            public MethodParameterDescription Description;
            public IEmittingCodec Codec;
            public LocalBuilder LocalVariable;
        }

        private static readonly Type[] ParameterTypes = new[] {typeof(object), typeof (byte[])};

        private readonly ICodecContainer codecContainer;

        public ServiceMethodHandlerFactory(ICodecContainer codecContainer)
        {
            this.codecContainer = codecContainer;
        }

        public ServiceMethodHandler CreateMethodHandler(ServiceDescription serviceDescription, ServicePath servicePath)
        {
            var serviceInterface = serviceDescription.Type;

            var dynamicMethod = new DynamicMethod(
                "__srpc__handle__" + serviceInterface.FullName + "__" + string.Join("_", servicePath),
                typeof(byte[]), ParameterTypes, Assembly.GetExecutingAssembly().ManifestModule, true);
            var il = dynamicMethod.GetILGenerator();
            var locals = new LocalVariableCollection(il, true);
            
            il.Emit(OpCodes.Ldarg_0);                                           // stack_0 = (TServiceImplementation) arg_0
            il.Emit(OpCodes.Castclass, serviceInterface);

            var serviceDesc = serviceDescription;
            for (int i = 1; i < servicePath.Length - 1; i++)
            {
                var propertyInfo = serviceDesc.Type.GetProperty(servicePath[i]);
                il.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());         // stack_0 = stack_0.Property
                SubserviceDescription subserviceDescription;
                if (!serviceDesc.TryGetSubservice(servicePath[i], out subserviceDescription))
                    throw new InvalidPathException();
                serviceDesc = subserviceDescription.Service;
            }

            var methodName = servicePath.MethodName;
            MethodDescription methodDesc;
            if (!serviceDesc.TryGetMethod(methodName, out methodDesc))
                throw new InvalidPathException();

            bool hasRetval = methodDesc.ReturnType != typeof(void);
            var parameters = methodDesc.Parameters.Select((x, i) => new ParameterNecessity
                {
                    Description = x,
                    Codec = codecContainer.GetEmittingCodecFor(x.Type),
                    LocalVariable = x.Way != MethodParameterWay.Val ? il.DeclareLocal(x.Type) : null
                })
                .ToArray();

            var requestParameters = parameters
                    .Where(x => x.Description.Way == MethodParameterWay.Val || x.Description.Way == MethodParameterWay.Ref)
                    .ToArray();

            var responseParameters = parameters
                .Where(x => x.Description.Way == MethodParameterWay.Ref || x.Description.Way == MethodParameterWay.Out)
                .ToArray();

            if (requestParameters.Any())
            {
                il.Emit(OpCodes.Ldarg_1);                                   // remainingBytes = arg_1.Length
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Stloc, locals.RemainingBytes);
                var pinnedVar = il.Emit_PinArray(typeof(byte), 1);         // var pinned dataPointer = pin(arg_1)
                il.Emit(OpCodes.Ldloc, pinnedVar);                         // data = dataPointer
                il.Emit(OpCodes.Stloc, locals.DataPointer);
            }

            foreach (var parameter in parameters)
            {
                switch (parameter.Description.Way)
                {
                    case MethodParameterWay.Val:
                        parameter.Codec.EmitDecode(il, locals, false);      // stack_i = decode(data, remainingBytes, false)
                        break;
                    case MethodParameterWay.Ref:
                        parameter.Codec.EmitDecode(il, locals, false);      // param_i = decode(data, remainingBytes, false)
                        il.Emit(OpCodes.Stloc, parameter.LocalVariable);
                        il.Emit(OpCodes.Ldloca, parameter.LocalVariable);   // stack_i = *param_i
                        break;
                    case MethodParameterWay.Out:
                        il.Emit(OpCodes.Ldloca, parameter.LocalVariable);   // stack_i = *param_i
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            il.Emit(OpCodes.Callvirt, methodDesc.MethodInfo);               // stack_0 = stack_0.Method(stack_1, stack_2, ...)

            if (hasRetval || responseParameters.Any())
            {
                IEmittingCodec retvalCodec = null;
                LocalBuilder retvalVar = null;
                
                if (hasRetval)
                {
                    retvalCodec = codecContainer.GetEmittingCodecFor(methodDesc.ReturnType);
                    retvalVar = il.DeclareLocal(methodDesc.ReturnType);             // var ret = stack_0
                    il.Emit(OpCodes.Stloc, retvalVar);
                    retvalCodec.EmitCalculateSize(il, retvalVar);                   // stack_0 = calculateSize(ret)
                }

                bool hasSizeOnStack = hasRetval;
                foreach (var parameter in responseParameters)
                {
                    parameter.Codec.EmitCalculateSize(il, parameter.LocalVariable); // stack_0 += calculateSize(param_i)
                    if (hasSizeOnStack)
                        il.Emit(OpCodes.Add);
                    else
                        hasSizeOnStack = true;
                }

                var dataArrayVar = il.DeclareLocal(typeof(byte[]));                 // var dataArray = new byte[size of retval]
                il.Emit(OpCodes.Newarr, typeof(byte));
                il.Emit(OpCodes.Stloc, dataArrayVar);

                var pinnedVar = il.Emit_PinArray(typeof(byte), dataArrayVar);       // var pinned dataPointer = pin(dataArrayVar)
                il.Emit(OpCodes.Ldloc, pinnedVar);                                  // data = dataPointer
                il.Emit(OpCodes.Stloc, locals.DataPointer);

                foreach (var parameter in responseParameters)
                    parameter.Codec.EmitEncode(il, locals, parameter.LocalVariable);// encode(data, param_i)

                if (hasRetval)
                    retvalCodec.EmitEncode(il, locals, retvalVar);                  // encode(data, ret)

                il.Emit(OpCodes.Ldloc, dataArrayVar);                               // stack_0 = dataArray
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, 0);                                         // stack_0 = new byte[0]
                il.Emit(OpCodes.Newarr, typeof(byte));
            }

            il.Emit(OpCodes.Ret);
            return (ServiceMethodHandler)dynamicMethod.CreateDelegate(typeof(ServiceMethodHandler));
        }
    }
}
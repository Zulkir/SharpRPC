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

using System.Collections.Generic;
using System.Reflection.Emit;
using SharpRpc.Reflection;

namespace SharpRpc.ServerSide.Handler
{
    public class HandlerClassBuildingContext
    {
        public IReadOnlyList<ServiceDescription> ServiceDescriptionChain { get; private set; }
        public MethodDescription MethodDescription { get; private set; }
        public TypeBuilder Builder { get; private set; }
        public GenericTypeParameterBuilder[] GenericTypeParameterBuilders { get; private set; }
        public HandlerClassFieldCache Fields { get; private set; }

        public HandlerClassBuildingContext(IReadOnlyList<ServiceDescription> serviceDescriptionChain, MethodDescription methodDescription, 
            TypeBuilder builder, GenericTypeParameterBuilder[] genericTypeParameterBuilders, HandlerClassFieldCache fields)
        {
            ServiceDescriptionChain = serviceDescriptionChain;
            MethodDescription = methodDescription;
            Builder = builder;
            GenericTypeParameterBuilders = genericTypeParameterBuilders;
            Fields = fields;
        } 
    }
}
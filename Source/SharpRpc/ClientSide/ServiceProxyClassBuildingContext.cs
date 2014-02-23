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
using SharpRpc.Codecs;

namespace SharpRpc.ClientSide
{
    public class ServiceProxyClassBuildingContext : IServiceProxyClassBuildingContext
    {
        public TypeBuilder Builder { get; private set; }
        public Type InterfaceType { get; private set; }
        public FieldBuilder ProcessorField { get; private set; }
        public FieldBuilder ScopeField { get; private set; }
        public FieldBuilder TimeoutSettingsField { get; private set; }
        public FieldBuilder CodecContainerField { get; private set; }
        public FieldBuilder ManualCodecsField { get; private set; }

        private readonly List<Type> manualCodecTypes; 

        public ServiceProxyClassBuildingContext(TypeBuilder builder, Type interfaceType)
        {
            InterfaceType = interfaceType;
            Builder = builder;

            manualCodecTypes = new List<Type>();

            ProcessorField = Builder.DefineField("methodCallProcessor", typeof(IOutgoingMethodCallProcessor), FieldAttributes.Private | FieldAttributes.InitOnly);
            ScopeField = Builder.DefineField("scope", typeof(string), FieldAttributes.Private | FieldAttributes.InitOnly);
            TimeoutSettingsField = Builder.DefineField("timeoutSettings", typeof(TimeoutSettings), FieldAttributes.Private | FieldAttributes.InitOnly);
            CodecContainerField = Builder.DefineField("codecContainer", typeof(ICodecContainer), FieldAttributes.Private | FieldAttributes.InitOnly);
            ManualCodecsField = Builder.DefineField("manualCodecs", typeof(IManualCodec[]), FieldAttributes.Private | FieldAttributes.InitOnly);
        }

        public int GetManualCodecIndex(Type type)
        {
            int index = manualCodecTypes.IndexOf(type);
            if (index != -1)
                return index;
            manualCodecTypes.Add(type);
            return manualCodecTypes.Count - 1;
        }

        public Type[] GetAllManualCodecTypes()
        {
            return manualCodecTypes.ToArray();
        }
    }
}

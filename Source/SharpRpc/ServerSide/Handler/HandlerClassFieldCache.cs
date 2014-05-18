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

namespace SharpRpc.ServerSide.Handler
{
    public class HandlerClassFieldCache
    {
        private readonly TypeBuilder typeBuilder;
        private readonly Dictionary<Type, FieldBuilder> manualCodecFields;
        private int manualCodecFieldNameDisambiguator = 0;

        public FieldBuilder CodecContainer { get; private set; }

        public HandlerClassFieldCache(TypeBuilder typeBuilder)
        {
            this.typeBuilder = typeBuilder;
            manualCodecFields = new Dictionary<Type, FieldBuilder>();
            CodecContainer = typeBuilder.DefineField("codecContainer", typeof(ICodecContainer), FieldAttributes.Private | FieldAttributes.InitOnly);
        }

        public FieldBuilder GetOrCreateManualCodec(Type type)
        {
            FieldBuilder field;
            if (!manualCodecFields.TryGetValue(type, out field))
            {
                field = typeBuilder.DefineField(string.Format("codecFor_{0}_{1}", type.Name, manualCodecFieldNameDisambiguator++), 
                    typeof(IManualCodec<>).MakeGenericType(type), FieldAttributes.Private | FieldAttributes.InitOnly);
                manualCodecFields.Add(type, field);
            }
            return field;
        }

        public IEnumerable<FieldBuilder> GetAllManualCodecFields()
        {
            return manualCodecFields.Values;
        }  
    }
}
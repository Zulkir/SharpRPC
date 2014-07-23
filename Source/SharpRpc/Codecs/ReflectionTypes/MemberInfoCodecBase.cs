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
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpRpc.Codecs.ReflectionTypes
{
    public abstract unsafe class MemberInfoCodecBase<T> : IManualCodec<T> where T : MemberInfo
    {
        private readonly IManualCodec<string> stringCodec;
        private readonly IManualCodec<Type> typeCodec;

        protected MemberInfoCodecBase(ICodecContainer codecContainer)
        {
            stringCodec = codecContainer.GetManualCodecFor<string>();
            typeCodec = codecContainer.GetManualCodecFor<Type>();
        }

        public Type Type { get { return typeof(MethodInfo); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        protected abstract IEnumerable<T> GetMembers(Type type);

        protected virtual void Validate(T value)
        { 
        }

        protected virtual int CalculateAdditionalSize(T value)
        {
            return 0;
        }

        protected virtual void EncodeAdditional(ref byte* data, T value)
        {
        }

        protected virtual T FinishDecoding(Type reflectedType, string name, IEnumerable<T> candidates, ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var singleOrDefault = candidates.SingleOrDefault();
            if (singleOrDefault == null)
                throw new InvalidDataException(string.Format("{0} '{1}.{2}' was not found", typeof(T).Name, reflectedType, name));
            return singleOrDefault;
        }

        public int CalculateSize(T value)
        {
            if (value == null)
                return typeCodec.CalculateSize(null);
            Validate(value);

            int result = 0;
            result += typeCodec.CalculateSize(value.ReflectedType);
            result += typeCodec.CalculateSize(value.DeclaringType);
            result += stringCodec.CalculateSize(value.Name);
            result += CalculateAdditionalSize(value);
            return result;
        }

        public void Encode(ref byte* data, T value)
        {
            if (value == null)
            {
                typeCodec.Encode(ref data, null);
                return;
            }
            Validate(value);

            typeCodec.Encode(ref data, value.ReflectedType);
            typeCodec.Encode(ref data, value.DeclaringType);
            stringCodec.Encode(ref data, value.Name);
            EncodeAdditional(ref data, value);
        }

        public T Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var reflectedType = typeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            if (reflectedType == null)
                return null;
            var declareingType = typeCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            var name = stringCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);

            var candidates = GetMembers(reflectedType).Where(x => x.Name == name && x.DeclaringType == declareingType);
            return FinishDecoding(reflectedType, name, candidates, ref data, ref remainingBytes, doNotCheckBounds);
        }
    }
}
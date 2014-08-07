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

namespace SharpRpc.Codecs.ReflectionTypes
{
    public unsafe class MemberInfoCodec : IManualCodec<MemberInfo>
    {
        private readonly IManualCodec<MemberTypes> memberTypesCodec;
        private readonly IManualCodec<ConstructorInfo> constructorInfoCodec;
        private readonly IManualCodec<EventInfo> eventInfoCodec;
        private readonly IManualCodec<FieldInfo> fieldInfoCodec;
        private readonly IManualCodec<MethodInfo> methodInfoCodec;
        private readonly IManualCodec<PropertyInfo> propertyInfoCodec;

        public Type Type { get { return typeof(MemberInfo); } }
        public int? FixedSize { get { return null; } }
        public int? MaxSize { get { return null; } }

        public MemberInfoCodec(ICodecContainer codecContainer)
        {
            memberTypesCodec = codecContainer.GetManualCodecFor<MemberTypes>();
            constructorInfoCodec = codecContainer.GetManualCodecFor<ConstructorInfo>();
            eventInfoCodec = codecContainer.GetManualCodecFor<EventInfo>();
            fieldInfoCodec = codecContainer.GetManualCodecFor<FieldInfo>();
            methodInfoCodec = codecContainer.GetManualCodecFor<MethodInfo>();
            propertyInfoCodec = codecContainer.GetManualCodecFor<PropertyInfo>();
        }

        public int CalculateSize(MemberInfo value)
        {
            int result = 0;
            result += memberTypesCodec.CalculateSize(value.MemberType);
            switch (value.MemberType)
            {
                case MemberTypes.Constructor:
                    result += constructorInfoCodec.CalculateSize((ConstructorInfo)value);
                    break;
                case MemberTypes.Event:
                    result += eventInfoCodec.CalculateSize((EventInfo)value);
                    break;
                case MemberTypes.Field:
                    result += fieldInfoCodec.CalculateSize((FieldInfo)value);
                    break;
                case MemberTypes.Method:
                    result += methodInfoCodec.CalculateSize((MethodInfo)value);
                    break;
                case MemberTypes.Property:
                    result += propertyInfoCodec.CalculateSize((PropertyInfo)value);
                    break;
                default:
                    throw new NotSupportedException(string.Format("MemberInfo type '{0}' is not spported for encoding/decoding", value.MemberType));
            }
            return result;
        }

        public void Encode(ref byte* data, MemberInfo value)
        {
            memberTypesCodec.Encode(ref data, value.MemberType);
            switch (value.MemberType)
            {
                case MemberTypes.Constructor:
                    constructorInfoCodec.Encode(ref data, (ConstructorInfo)value);
                    return;
                case MemberTypes.Event:
                    eventInfoCodec.Encode(ref data, (EventInfo)value);
                    return;
                case MemberTypes.Field:
                    fieldInfoCodec.Encode(ref data, (FieldInfo)value);
                    return;
                case MemberTypes.Method:
                    methodInfoCodec.Encode(ref data, (MethodInfo)value);
                    return;
                case MemberTypes.Property:
                    propertyInfoCodec.Encode(ref data, (PropertyInfo)value);
                    return;
                default:
                    throw new NotSupportedException(string.Format("MemberType '{0}' is not spported for encoding/decoding", value.MemberType));
            }
        }

        public MemberInfo Decode(ref byte* data, ref int remainingBytes, bool doNotCheckBounds)
        {
            var type = memberTypesCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
            switch (type)
            {
                case MemberTypes.Constructor:
                    return constructorInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                case MemberTypes.Event:
                    return eventInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                case MemberTypes.Field:
                    return fieldInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                case MemberTypes.Method:
                    return methodInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                case MemberTypes.Property:
                    return propertyInfoCodec.Decode(ref data, ref remainingBytes, doNotCheckBounds);
                default:
                    throw new NotSupportedException(string.Format("MemberType '{0}' is not spported for encoding/decoding", type));
            }
        }
    }
}
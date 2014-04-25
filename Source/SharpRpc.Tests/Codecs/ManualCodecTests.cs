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

using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    [TestFixture]
    public unsafe class ManualCodecTests
    {
        public class MyClass
        {
            public int Value { get; set; }

            public MyClass(int value)
            {
                Value = value;
            }

            public static readonly ConstructorInfo Constructor = typeof (MyClass).GetConstructor(new[] {typeof (int)});
        }

        [Test]
        public void CalculateSize()
        {
            var item = new MyClass(222);
            const int size = 123123;

            var emittingCodec = Substitute.For<IEmittingCodec>();
            emittingCodec.WhenForAnyArgs(x => x.EmitCalculateSize(null, null)).Do(x =>
                {
                    var context = x.Arg<IEmittingContext>();
                    context.IL.Ldc_I4(size);
                });
            var manualCodec = new ManualCodec<MyClass>(null, emittingCodec);
           
            Assert.That(manualCodec.CalculateSize(item), Is.EqualTo(size));
        }

        [Test]
        public void Encode()
        {
            const int expectedValue = 123123;
            const int expectedPointerDistance = 234;

            var emittingCodec = Substitute.For<IEmittingCodec>();
            emittingCodec.WhenForAnyArgs(x => x.EmitEncode(null, 0))
                .Do(x =>
                    {
                        var context = x.Arg<IEmittingContext>();
                        var il = context.IL;
                        il.Ldloc(context.DataPointerVar);
                        il.Ldc_I4(expectedValue);
                        il.Stind_I4();
                        il.IncreasePointer(context.DataPointerVar, expectedPointerDistance);
                    });
            var manualCodec = new ManualCodec<MyClass>(null, emittingCodec);

            int value;
            long pointerDistance;

            var data = new byte[4];
            fixed (byte* pData = data)
            {
                var p = pData;
                manualCodec.Encode(ref p, new MyClass(222));
                value = *(int*)pData;
                pointerDistance = p - pData;
            }

            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(pointerDistance, Is.EqualTo(expectedPointerDistance));
        }

        [Test]
        public void Decode()
        {
            DoTestDecode(false);
        }

        [Test]
        public void DecodeFast()
        {
            DoTestDecode(true);
        }

        private void DoTestDecode(bool fast)
        {
            const int expectedValue = 123123;
            const int expectedPointerDistance = 234;
            const int expetedRemainingBytesDistance = 345;

            var emittingCodec = Substitute.For<IEmittingCodec>();
            emittingCodec.When(x => x.EmitDecode(Arg.Any<IEmittingContext>(), fast)).Do(x =>
                {
                    var context = x.Arg<EmittingContextBase>();
                    var il = context.IL;
                    il.Ldc_I4(expectedValue);
                    il.Newobj(MyClass.Constructor);
                    il.IncreasePointer(context.DataPointerVar, expectedPointerDistance);
                    il.DecreaseInteger(context.RemainingBytesVar, expetedRemainingBytesDistance);
                });
            var manualCodec = new ManualCodec<MyClass>(null, emittingCodec);

            int value;
            int pointerDistance;
            int remainingBytesDistance;

            var data = new byte[4];
            fixed (byte* pData = data)
            {
                var p = pData;
                int remainingBytes = 0;
                value = manualCodec.Decode(ref p, ref remainingBytes, fast).Value;
                pointerDistance = (int)(p - pData);
                remainingBytesDistance = -remainingBytes;
            }

            Assert.That(value, Is.EqualTo(expectedValue));
            Assert.That(pointerDistance, Is.EqualTo(expectedPointerDistance));
            Assert.That(remainingBytesDistance, Is.EqualTo(expetedRemainingBytesDistance));
        }

        [Test]
        public void UseOwnCodecContainer()
        {
            var innerCodec = Substitute.For<IManualCodec<int>>();

            var codecContainer = Substitute.For<ICodecContainer>();
            codecContainer.GetManualCodecFor<int>().Returns(innerCodec);

            var emittingCodec = Substitute.For<IEmittingCodec>();
            emittingCodec.WhenForAnyArgs(x => x.EmitCalculateSize(null, null)).Do(x =>
            {
                var context = x.Arg<IEmittingContext>();
                context.EmitLoadManualCodecFor(typeof(int));
                context.IL.Callvirt(typeof(object).GetMethod("GetHashCode"));
            });
            
            var manualCodec = new ManualCodec<object>(codecContainer, emittingCodec);
            var result = manualCodec.CalculateSize(null);

            Assert.That(result, Is.EqualTo(innerCodec.GetHashCode()));
        }
    }
}
using System.Runtime.Serialization;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class RecursiveDataContractCodecTests : CodecTestsBase
    {
        #region Contracts

        [DataContract]
        public class BinaryTreeNode
        {
            [DataMember] public int Value { get; set; }
            [DataMember] private int PrivateValue { get; set; }
            [DataMember] public BinaryTreeNode Left { get; set; }
            [DataMember] public BinaryTreeNode Right { get; set; }

            public BinaryTreeNode(int value, int privateValue, BinaryTreeNode left, BinaryTreeNode right)
            {
                Value = value;
                PrivateValue = privateValue;
                Left = left;
                Right = right;
            }

            public bool Equals(BinaryTreeNode other) { return other != null && Value == other.Value && PrivateValue == other.PrivateValue && Equals(Left, other.Left) && Equals(Right, other.Right); }
            public override bool Equals(object obj) { return obj is BinaryTreeNode && Equals((BinaryTreeNode)obj); }
            public override int GetHashCode() { return 0; }
        }

        [DataContract]
        public class Even
        {
            [DataMember] 
            public Odd Odd { get; set; }

            public Even(Odd odd)
            {
                Odd = odd;
            }

            public bool Equals(Even other) { return other != null && Equals(Odd, other.Odd); }
            public override bool Equals(object obj) { return obj is Even && Equals((Even)obj); }
            public override int GetHashCode() { return 0; }
        }

        [DataContract]
        public class Odd
        {
            [DataMember] 
            public Even Even { get; set; }

            public Odd(Even even)
            {
                Even = even;
            }

            public bool Equals(Odd other) { return other != null && Equals(Even, other.Even); }
            public override bool Equals(object obj) { return obj is Odd && Equals((Odd)obj); }
            public override int GetHashCode() { return 0; }
        }

        #endregion

        private ICodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        private void DoTest<T>(T value) where T : class
        {
            DoTest(new /*RecursiveDataContractCodec*/DirectDataContractCodec(typeof(T), codecContainer), value, (o1, o2) =>
            {
                if (ReferenceEquals(o1, null))
                    Assert.That(o2, Is.Null);
                else
                    Assert.That(o2, Is.EqualTo(o1));
            });
        }

        [Test]
        [Ignore]
        public void DirectRecursion()
        {
            DoTest((BinaryTreeNode)null);
            DoTest(new BinaryTreeNode(123, 234, null, null));
            DoTest(new BinaryTreeNode(123, 234, new BinaryTreeNode(345, 456, null, new BinaryTreeNode(111, 222, null, null)), null));
            DoTest(
                new BinaryTreeNode(123, 234, 
                    new BinaryTreeNode(222, 333, 
                        new BinaryTreeNode(444, 555,
                            new BinaryTreeNode(456, 567, null, null), 
                            new BinaryTreeNode(423, 543, null, null)), 
                        null), 
                    new BinaryTreeNode(385, 926,
                        null,
                        new BinaryTreeNode(835, 195,
                            new BinaryTreeNode(987, 752,
                                null,
                                new BinaryTreeNode(852, 147,
                                    new BinaryTreeNode(954, 753, null, null), 
                                    null)),
                            null))));
        }

        [Test]
        [Ignore]
        public void IndirectRecursion()
        {
            DoTest((Even)null);
            DoTest(new Even(null));
            DoTest(new Even(new Odd(null)));
            DoTest(new Even(new Odd(new Even(null))));
            DoTest(new Even(new Odd(new Even(new Odd(null)))));
            DoTest(new Even(new Odd(new Even(new Odd(new Even(null))))));
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using SharpRpc.Codecs;

namespace SharpRpc.Tests.Codecs
{
    public class CollectionCodecTests : CodecTestsBase
    {
        #region Custom Types
        [DataContract]
        public class MyContract : IEquatable<MyContract>
        {
            [DataMember]
            public double A { get; set; }

            [DataMember]
            public string B { get; set; }

            public MyContract(double a, string b) { A = a; B = b; }
            public bool Equals(MyContract other) { return A == other.A && B == other.B; }
            public override bool Equals(object obj) { return obj is MyContract && Equals((MyContract)obj); }
            public override int GetHashCode() { return 0; }
            public override string ToString() { return string.Format("[{0}; {1}]", A, B); }
        }

        public class MyCollection<T> : ICollection<T>
        {
            private readonly List<T> list = new List<T>();

            public IEnumerator<T> GetEnumerator() { return list.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return list.GetEnumerator(); }
            public void Add(T item) { list.Add(item); }
            public void Clear() { list.Clear(); }
            public bool Contains(T item) { return list.Contains(item); }
            public void CopyTo(T[] array, int arrayIndex) { list.CopyTo(array, arrayIndex); }
            public bool Remove(T item) { return list.Remove(item); }
            public int Count { get { return list.Count; } }
            public bool IsReadOnly { get { return ((ICollection<T>)list).IsReadOnly; } }
        }
        #endregion

        private ICodecContainer codecContainer;

        [SetUp]
        public void Setup()
        {
            codecContainer = new CodecContainer();
        }

        private void DoTest<TCollection, TElement>(TCollection collection) where TCollection : class, ICollection<TElement>
        {
            DoTest(new CollectionCodec(typeof(TCollection), typeof(TElement), codecContainer), collection, (b, a) =>
                {
                    if (a == null)
                        Assert.That(b, Is.Null);
                    else
                        Assert.That(b, Is.EquivalentTo(a));
                });
        }

        private void DoTestBasic<TCollection, TElement>() where TCollection : class, ICollection<TElement>, new()
        {
            DoTest<TCollection, TElement>(null);
            DoTest<TCollection, TElement>(new TCollection());
        }

        private void DoTestValueType<TCollection>() where TCollection : class, ICollection<int>, new()
        {
            DoTestBasic<TCollection, int>();
            DoTest<TCollection, int>(new TCollection { 123, 234, 345 });
        }

        private void DoTestReferenceType<TCollection>() where TCollection : class, ICollection<MyContract>, new()
        {
            DoTestBasic<TCollection, MyContract>();
            DoTest<TCollection, MyContract>(new TCollection { new MyContract(12.34, "asdasd"), null, new MyContract(23.45, "uihifjn"), null});
        }

        [Test]
        public void List()
        {
            DoTestValueType<List<int>>();
            DoTestReferenceType<List<MyContract>>();
        }

        [Test]
        public void LinkedList()
        {
            DoTestValueType<LinkedList<int>>();
            DoTestReferenceType<LinkedList<MyContract>>();
        }

        [Test]
        public void Custom()
        {
            DoTestValueType<MyCollection<int>>();
            DoTestReferenceType<MyCollection<MyContract>>();
        }

        [Test]
        public void Dictionary()
        {
            DoTestBasic<Dictionary<int, int>, KeyValuePair<int, int>>();
            DoTest<Dictionary<int, int>, KeyValuePair<int, int>>(new Dictionary<int, int> { {12, 34}, { 23, 34} });
            DoTestBasic<Dictionary<int, MyContract>, KeyValuePair<int, MyContract>>();
            DoTest<Dictionary<int, MyContract>, KeyValuePair<int, MyContract>>(new Dictionary<int, MyContract> { { 1, new MyContract(12.34, "asd")}, { 2, null } });
            DoTestBasic<Dictionary<string, MyContract>, KeyValuePair<string, MyContract>>();
            DoTest<Dictionary<string, MyContract>, KeyValuePair<string, MyContract>>(new Dictionary<string, MyContract> { { "1", new MyContract(12.34, "asd") }, { "2", null } });
        }
    }
}
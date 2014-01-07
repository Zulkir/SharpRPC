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
using System.Linq;
using NUnit.Framework;
using SharpRpc.Topology;

namespace SharpRpc.Tests
{
    [TestFixture]
    public class TopologyParserTests
    {
        private TopologyParser parser; 

        [SetUp]
        public void Setup()
        {
            parser = new TopologyParser();
        }

        [Test]
        public void Different()
        {
            const string text = @"Storage			     single	      http://myhost:7000
                                  OrganizationDispatcher even         http://myhost:7020, http://myhost:7120, http://myhost:7220, http://myhost:7320 

                                  MappedService          map          @null http://myhost:7123, scope234 http://myhost:7234, scope345 http://myhost:7345, scope456 http://myhost:7456

                                  HealthService          endpoint";
            var topology = parser.Parse(text);
            //Assert.That(parser.TryParse(text, out topology), Is.True);
            AssertEndPoint(topology, "Storage", null, new ServiceEndPoint("http", "myhost", 7000));
            AssertEndPoint(topology, "OrganizationDispatcher", null, new ServiceEndPoint("http", "myhost", 7020));
            AssertEndPoint(topology, "OrganizationDispatcher", GetStringWithHashCode(0), new ServiceEndPoint("http", "myhost", 7020));
            AssertEndPoint(topology, "OrganizationDispatcher", GetStringWithHashCode(1), new ServiceEndPoint("http", "myhost", 7120));
            AssertEndPoint(topology, "OrganizationDispatcher", GetStringWithHashCode(2), new ServiceEndPoint("http", "myhost", 7220));
            AssertEndPoint(topology, "OrganizationDispatcher", GetStringWithHashCode(3), new ServiceEndPoint("http", "myhost", 7320));
            AssertEndPoint(topology, "MappedService", null, new ServiceEndPoint("http", "myhost", 7123));
            AssertEndPoint(topology, "MappedService", "scope234", new ServiceEndPoint("http", "myhost", 7234));
            AssertEndPoint(topology, "MappedService", "scope345", new ServiceEndPoint("http", "myhost", 7345));
            AssertEndPoint(topology, "MappedService", "scope456", new ServiceEndPoint("http", "myhost", 7456));
            AssertEndPoint(topology, "HealthService", "http://myhost:1234", new ServiceEndPoint("http", "myhost", 1234));
        }

        [Test]
        public void Random()
        {
            const string text = @"Print random http://myhost:7008, http://myhost:7118, http://myhost:7228";
            var topology = parser.Parse(text);

            int c0 = 0, c1 = 0, c2 = 0;
            for (int i = 0; i < 100; i++)
            {
                IServiceTopology serviceTopology;
                ServiceEndPoint endPoint;
                Assert.That(topology.TryGetValue("Print", out serviceTopology), Is.True);
                Assert.That(serviceTopology.TryGetEndPoint(null, out endPoint), Is.True);
                switch (endPoint.Port)
                {
                    case 7008: c0++; break;
                    case 7118: c1++; break;
                    case 7228: c2++; break;
                }
            }

            Assert.That(c0, Is.GreaterThan(0));
            Assert.That(c1, Is.GreaterThan(0));
            Assert.That(c2, Is.GreaterThan(0));
        }

        [Test]
        public void EvenDistribution()
        {
            var dict = new Dictionary<string, int>
                {
                    {"http://myhost:7020", 0},
                    {"http://myhost:7120", 0},
                    {"http://myhost:7220", 0},
                    {"http://myhost:7320", 0},
                    {"http://myhost:7420", 0},
                };

            const string text = @"OrganizationDispatcher even http://myhost:7020, http://myhost:7120, http://myhost:7220, http://myhost:7320, http://myhost:7420";
            var topology = parser.Parse(text);

            for (int i = 0; i < 10000; i++)
            {
                var scope = string.Format("{0};{1}", Guid.NewGuid(), Guid.NewGuid());
                dict[topology.GetEndPoint("OrganizationDispatcher", scope).ToString()]++;
            }

            double avgHits = dict.Values.Average();

            foreach (var hits in dict.Values)
                Assert.That(Math.Abs(hits - avgHits), Is.LessThan(0.05 * avgHits));
        }

        private static string GetStringWithHashCode(int hashCode)
        {
            switch (hashCode)
            {
                case 0: return "";
                case 1: return new string((char)1, 1);
                case 2: return new string((char)2, 1);
                case 3: return new string((char)3, 1);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private static void AssertEndPoint(IReadOnlyDictionary<string, IServiceTopology> topology, string serviceName, string scope, ServiceEndPoint expectedEndPoint)
        {
            IServiceTopology serviceTopology;
            ServiceEndPoint endPoint;
            Assert.That(topology.TryGetValue(serviceName, out serviceTopology), Is.True);
            Assert.That(serviceTopology.TryGetEndPoint(scope, out endPoint), Is.True);
            Assert.That(endPoint, Is.EqualTo(expectedEndPoint));
        }
    }
}
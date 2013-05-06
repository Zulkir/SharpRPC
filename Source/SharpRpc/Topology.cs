#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SharpRpc
{
    public class Topology : ITopology
    {
        private readonly ConcurrentDictionary<string, List<ServiceEndPoint>> endPoints; 

        public Topology()
        {
            endPoints = new ConcurrentDictionary<string, List<ServiceEndPoint>>();
        }

        public void AddEndPoint(string serviceName, string scope, ServiceEndPoint endPoint)
        {
            var list = endPoints.GetOrAdd(serviceName, n => new List<ServiceEndPoint>());
            list.Add(endPoint);
        }

        public bool TryGetEndPoint(string serviceName, string scope, out ServiceEndPoint endPoint)
        {
            List<ServiceEndPoint> list;
            if (!endPoints.TryGetValue(serviceName, out list) || list.Count == 0)
            {
                endPoint = default(ServiceEndPoint);
                return false;
            }
            endPoint = scope == null ? list[0] : list[scope[scope.Length - 1] % list.Count];
            return true;
        }
    }
}

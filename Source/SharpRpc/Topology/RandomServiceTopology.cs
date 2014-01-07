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

namespace SharpRpc.Topology
{
    public class RandomServiceTopology : IServiceTopology
    {
        private readonly ServiceEndPoint[] endPoints;
        private readonly Random random = new Random();

        public RandomServiceTopology(params ServiceEndPoint[] endPoints) 
            : this(endPoints as IEnumerable<ServiceEndPoint>) {}

        public RandomServiceTopology(IEnumerable<ServiceEndPoint> endPoints)
        {
            if (endPoints == null)
                throw new ArgumentNullException("endPoints");

            this.endPoints = endPoints.ToArray();

            if (this.endPoints.Length == 0)
                throw new ArgumentException("End point collection must have at least one element", "endPoints");
        }

        public IEnumerable<ServiceEndPoint> GetAllKnownEndPoints()
        {
            return endPoints;
        }

        public bool TryGetEndPoint(string scope, out ServiceEndPoint endPoint)
        {
            int index = random.Next(0, endPoints.Length);
            endPoint = endPoints[index];
            return true;
        }
    }
}

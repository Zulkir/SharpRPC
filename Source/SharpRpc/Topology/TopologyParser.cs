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
using System.Text.RegularExpressions;

namespace SharpRpc.Topology
{
    public class TopologyParser : ITopologyParser
    {
        private static readonly char[] LineBreaks = new[] { '\r', '\n' };
        private static readonly char[] Comma = new [] {','};
        private static readonly Regex ServiceTopologyRegex = new Regex(@"^(\w+)\s+(\w+)(\s+([^\s].*))?$");
        private static readonly Regex MapElementRegex = new Regex(@"^([\@\w]+)\s+([^\s]+)$");

        public IReadOnlyDictionary<string, IServiceTopology> Parse(string text)
        {
            var lines = text.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries);
            return lines.Select(ParseServiceNameAndTopology).ToDictionary(x => x.Key, x => x.Value);
        }

        private static KeyValuePair<string, IServiceTopology> ParseServiceNameAndTopology(string line)
        {
            var match = ServiceTopologyRegex.Match(line.Trim());
            if (!match.Success)
                throw new InvalidDataException(string.Format("'{0}' is not a valid service topology description", line));
            var serviceName = match.Groups[1].Value;
            var serviceTopologyType = match.Groups[2].Value;
            var endPointsText = match.Groups[4].Value;

            IServiceTopology serviceTopology;
            switch (serviceTopologyType)
            {
                case "single": serviceTopology = ParseSingleServiceTopology(endPointsText); break;
                case "even": serviceTopology = ParseEvenlyDistibutedServiceTopology(endPointsText); break;
                case "random": serviceTopology = ParseRandomServiceTopology(endPointsText); break;
                case "map": serviceTopology = ParseMapServiceTopology(endPointsText); break;
                case "endpoint": serviceTopology = new EndPointServiceTopology(); break;
                default: throw new InvalidDataException(string.Format("Unknown service topology type '{0}'", serviceTopologyType));
            }

            return new KeyValuePair<string, IServiceTopology>(serviceName, serviceTopology);
        }

        private static IServiceTopology ParseSingleServiceTopology(string endPointsText)
        {
            return new SingleHostServiceTopology(ServiceEndPoint.Parse(endPointsText.Trim()));
        }

        private static IServiceTopology ParseEvenlyDistibutedServiceTopology(string endPointsText)
        {
            return new EvenlyDistributedServiceTopology(endPointsText.Split(Comma, StringSplitOptions.RemoveEmptyEntries).Select(x => ServiceEndPoint.Parse(x.Trim())));
        }

        private static IServiceTopology ParseRandomServiceTopology(string endPointsText)
        {
            return new RandomServiceTopology(endPointsText.Split(Comma, StringSplitOptions.RemoveEmptyEntries).Select(x => ServiceEndPoint.Parse(x.Trim())));
        }

        private static IServiceTopology ParseMapServiceTopology(string endPointsText)
        {
            var pairStrings = endPointsText.Split(Comma, StringSplitOptions.RemoveEmptyEntries);
            ServiceEndPoint? nullEndPoint = null;
            var pairs = new List<KeyValuePair<string, ServiceEndPoint>>(pairStrings.Length);
            foreach (var pairString in pairStrings)
            {
                var match = MapElementRegex.Match(pairString.Trim());
                if (!match.Success)
                    throw new InvalidDataException(string.Format("'{0}' is not a valid map service topology element", pairString.Trim()));
                var key = match.Groups[1].Value;
                var endPoint = ServiceEndPoint.Parse(match.Groups[2].Value);
                if (key == "@null")
                    nullEndPoint = endPoint;
                else
                    pairs.Add(new KeyValuePair<string, ServiceEndPoint>(key, endPoint));
            }
            return new MapServiceTopology(nullEndPoint, pairs);
        }
    }
}
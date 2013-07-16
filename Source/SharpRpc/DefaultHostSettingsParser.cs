#region License
/*
Copyright (c) 2013 Daniil Rodin, Maxim Sannikov of Buhgalteria.Kontur team of SKB Kontur

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
using System.Text.RegularExpressions;
using System.Linq;

namespace SharpRpc
{
    public class DefaultHostSettingsParser : IHostSettingsParser
    {
        private static readonly char[] LineBreaks = new[] { '\r', '\n' };
        private static readonly char[] Whitespaces = new[] { ' ', '\t' };
        private static readonly Regex EndPointRegex = new Regex(@"^(\w+)://([^:]+):(\d+)$");

        public bool TryParse(string text, out IHostSettings settings)
        {
            if (text == null)
            {
                settings = null;
                return false;
            }
                
            var lines = text.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                settings = null;
                return false;
            }

            ServiceEndPoint endPoint;
            if (!TryParseEndPoint(lines[0], out endPoint))
            {
                settings = null;
                return false;
            }

            var pairs = new List<InterfaceImplementationTypePair>();
            foreach (var line in lines.Skip(1).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
            {
                InterfaceImplementationTypePair pair;
                if (TryParsePair(line, out pair))
                    pairs.Add(pair);
                else
                {
                    settings = null;
                    return false;
                }
            }

            settings = new HostSettings(endPoint, pairs);
            return true;
        }

        private static bool TryParseEndPoint(string endPointString, out ServiceEndPoint endPoint)
        {
            var match = EndPointRegex.Match(endPointString);
            if (!match.Success)
            {
                endPoint = default(ServiceEndPoint);
                return false;
            }

            var protocol = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            
            int port;
            if (!int.TryParse(match.Groups[3].Value, out port))
            {
                endPoint = default(ServiceEndPoint);
                return false;
            }

            endPoint = new ServiceEndPoint(protocol, value, port);
            return true;
        }

        private static bool TryParsePair(string line, out InterfaceImplementationTypePair pair)
        {
            var parts = line.Split(Whitespaces, StringSplitOptions.RemoveEmptyEntries);

            string interfaceAssemblyPath;
            string interfaceName;
            string implementationAssemblyPath;
            string implementationName;

            switch (parts.Length)
            {
                case 3:
                    interfaceAssemblyPath = implementationAssemblyPath = parts[0];
                    interfaceName = parts[1];
                    implementationName = parts[2];
                    break;
                case 4:
                    interfaceAssemblyPath = parts[0];
                    interfaceName = parts[1];
                    implementationAssemblyPath = parts[2];
                    implementationName = parts[3];
                    break;
                default:
                    pair = default(InterfaceImplementationTypePair);
                    return false;
            }

            var interfaceType = Type.GetType(interfaceName + ", " + interfaceAssemblyPath, false);
            var implementationType = Type.GetType(implementationName + ", " + implementationAssemblyPath, false);

            if (interfaceType == null || implementationType == null)
            {
                pair = default(InterfaceImplementationTypePair);
                return false;
            }

            pair = new InterfaceImplementationTypePair(interfaceType, implementationType);
            return true;
        }
    }
}
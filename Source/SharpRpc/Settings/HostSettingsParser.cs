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
using System.IO;
using System.Linq;

namespace SharpRpc.Settings
{
    public class HostSettingsParser : IHostSettingsParser
    {
        private static readonly char[] LineBreaks = new[] { '\r', '\n' };
        private static readonly char[] Whitespaces = new[] { ' ', '\t' };

        public IHostSettings Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            
            var lines = text.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                throw new InvalidDataException("Self end point was not found");

            var endPoint = ServiceEndPoint.Parse(lines[0]);
            var pairs = lines.Skip(1).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(ParsePair);

            return new HostSettings(endPoint, pairs);
        }

        private InterfaceImplementationTypePair ParsePair(string line)
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
                    throw new InvalidDataException(string.Format("'{0}' is not a valid interface-implementation description", line));
            }

            var interfaceType = Type.GetType(interfaceName + ", " + interfaceAssemblyPath, false);
            var implementationType = Type.GetType(implementationName + ", " + implementationAssemblyPath, false);

            if (interfaceType == null)
                throw new InvalidDataException(string.Format("Type '{0}' was not found", interfaceName));
                
            if(implementationType == null)
                throw new InvalidDataException(string.Format("Type '{0}' was not found", implementationName));
            
            return new InterfaceImplementationTypePair(interfaceType, implementationType);
        }
    }
}
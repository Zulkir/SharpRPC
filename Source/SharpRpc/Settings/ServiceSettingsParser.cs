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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpRpc.Settings
{
    public class ServiceSettingsParser : IServiceSettingsParser
    {       
        private static readonly char[] LineBreaks = new[] { '\r', '\n' };
        private static readonly Regex PairRegex = new Regex(@"^([^=]+)=([^=]+)$");

        public IReadOnlyDictionary<string, string> Parse(string text)
        {
            return text
                .Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x) && !x.StartsWith("#"))
                .Select(ParsePair)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private static KeyValuePair<string, string> ParsePair(string line)
        {
            var match = PairRegex.Match(line);
            if (!match.Success)
                throw new InvalidDataException(string.Format("'{0}' is not a valid setting", line));
            return new KeyValuePair<string, string>(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
        }
    }
}

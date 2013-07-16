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
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpRpc
{
    public class ServiceSettingsParser : IServiceSettingsParser
    {       
        private static readonly char[] LineBreaks = new[] { '\r', '\n' };
        private static readonly Regex PairRegex = new Regex(@"^([^=]+)=([^=]+)$");

        public bool TryParse(string text, out IReadOnlyDictionary<string, string> serviceSettings)
        {
            var lines = text.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries);
            var settings = new Dictionary<string, string>();
            foreach (var line in lines.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x) && !x.StartsWith("#")))
            {
                string key, value;
                if (!TryParsePair(line, out key, out value))
                {
                    serviceSettings = null;
                    return false;
                }
                settings.Add(key, value);
            }
            serviceSettings = settings;
            return true;
        }

        private static bool TryParsePair(string line, out string key, out string value)
        {
            var match = PairRegex.Match(line);
            if (!match.Success)
            {
                key = value = null;
                return false;
            }
            key = match.Groups[1].Value.Trim();
            value = match.Groups[2].Value.Trim();
            return true;
        }
    }
}

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

namespace SharpRpc
{
    public class TimeoutSettings : IEquatable<TimeoutSettings>
    {
        public int MaxMilliseconds { get; private set; }
        public int NotReadyRetryCount { get; private set; }
        public int NotReadyRetryMilliseconds { get; private set; }

        public TimeoutSettings(int maxMilliseconds, int retryCount, int retryMilliseconds)
        {
            MaxMilliseconds = maxMilliseconds;
            NotReadyRetryCount = retryCount;
            NotReadyRetryMilliseconds = retryMilliseconds;
        }

        public TimeoutSettings(int maxMilliseconds) : this(maxMilliseconds, int.MaxValue, 0) { }
        public TimeoutSettings(int retryCount, int retryMilliseconds) : this(int.MaxValue, retryCount, retryMilliseconds) { }
        public TimeoutSettings() : this(int.MaxValue, int.MaxValue, 0) { }
        
        public bool Equals(TimeoutSettings other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return MaxMilliseconds == other.MaxMilliseconds &&
                   NotReadyRetryCount == other.NotReadyRetryCount &&
                   NotReadyRetryMilliseconds == other.NotReadyRetryMilliseconds;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TimeoutSettings);
        }

        public override int GetHashCode()
        {
            return MaxMilliseconds + NotReadyRetryCount + NotReadyRetryMilliseconds;
        }

        public override string ToString()
        {
            return string.Format("{{MaxMilliseconds: {0}; NotReadyRetryCount: {1}; NotReadyRetryMilliseconds: {2}}}",
                MaxMilliseconds, NotReadyRetryCount, NotReadyRetryMilliseconds);
        }

        public static bool operator ==(TimeoutSettings s1, TimeoutSettings s2)
        {
            if (ReferenceEquals(s1, s2))
                return true;
            if (ReferenceEquals(s1, null))
                return false;
            return s1.Equals(s2);
        }

        public static bool operator !=(TimeoutSettings s1, TimeoutSettings s2)
        {
            return !(s1 == s2);
        }

        public static TimeoutSettings NoTimeout { get { return new TimeoutSettings(); }}
    }
}

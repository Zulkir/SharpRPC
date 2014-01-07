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

namespace SharpRpc.Settings
{
    public struct InterfaceImplementationTypePair : IEquatable<InterfaceImplementationTypePair>
    {
        public Type Interface;
        public Type ImplementationType;

        public InterfaceImplementationTypePair(Type serviceInterface, Type implementationType)
        {
            Interface = serviceInterface;
            ImplementationType = implementationType;
        }

        public bool Equals(InterfaceImplementationTypePair other)
        {
            return Interface == other.Interface && ImplementationType == other.ImplementationType;
        }

        public override bool Equals(object obj)
        {
            return obj is InterfaceImplementationTypePair && Equals((InterfaceImplementationTypePair)obj);
        }

        public override int GetHashCode()
        {
            return (Interface ?? typeof(object)).GetHashCode() ^ (ImplementationType ?? typeof(object)).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{{0} : {1}}}", Interface.FullName, ImplementationType.FullName);
        }
    }
}
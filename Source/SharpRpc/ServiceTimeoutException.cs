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
using SharpRpc.Interaction;

namespace SharpRpc
{
    public class ServiceTimeoutException : Exception
    {
         public ServiceTimeoutException(Request request, int timeout, Exception innerException)
             : base(string.Format("Request '{0}' for scope '{1}' has timed out ({2})", 
                request.Path, request.ServiceScope, timeout), innerException)
         {
             
         }

         public ServiceTimeoutException(Request request, int attempts, int retryMilliseconds)
             : base(string.Format("Request '{0}' for scope '{1}' has been attempted maximum number of times ({2}, {3}ms between each)", 
                request.Path, request.ServiceScope, attempts, retryMilliseconds))
         {

         }
    }
}

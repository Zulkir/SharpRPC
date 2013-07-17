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
using SharpRpc.Interaction;

namespace SharpRpc.Logs
{
    public class ConsoleLogger : ILogger
    {
        public void WriteIncoming(Request request)
        {
            Console.WriteLine("Incoming: {0} for scope '{1}'", request.Path, request.ServiceScope);
        }

        public void WriteFinishedSuccessfully(Request request, TimeSpan executionTime)
        {
            Console.WriteLine("Success: {0} for scope '{1}' within {2} ms", request.Path, request.ServiceScope, executionTime.TotalMilliseconds);
        }

        public void WriteFinishedWithBadStatus(Request request, ResponseStatus responseStatus)
        {
            Console.WriteLine("Error ({0}): {1} for scope '{2}'", responseStatus, request.Path, request.ServiceScope);
        }

        public void WriteFinishedWithException(Request request, Exception exception)
        {
            Console.WriteLine("Exception: {0} for scope '{1}'", request.Path, request.ServiceScope);
            do
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);
                Console.WriteLine("--- end of stack trace ---");
                exception = exception.InnerException;
            } 
            while (exception != null);
        }
    }
}
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

using SharpRpc.Utility;

namespace SharpRpc.Interaction
{
    public class Response
    {
        public ResponseStatus Status { get; private set; }
        public byte[] Data { get; private set; }

        public Response(ResponseStatus status, byte[] data)
        {
            Status = status;
            Data = data;
        }

        public static Response NotReady { get { return new Response(ResponseStatus.NotReady, CommonImmutables.EmptyBytes); } }
        public static Response BadRequest { get { return new Response(ResponseStatus.BadRequest, CommonImmutables.EmptyBytes); } }
        public static Response NotFound { get { return new Response(ResponseStatus.ServiceNotFound, CommonImmutables.EmptyBytes); } }
        public static Response InvalidImplementation { get { return new Response(ResponseStatus.InvalidImplementation, CommonImmutables.EmptyBytes); } }
        public static Response InternalError { get { return new Response(ResponseStatus.InternalServerError, CommonImmutables.EmptyBytes); } }
    }
}

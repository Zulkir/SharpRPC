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

namespace SharpRpc.Interaction
{
    public enum ResponseStatus
    {
        Ok = 200,
        NotReady = 202,
        BadRequest = 400,
        ServiceNotFound = 404,
        Exception = 409,
        InvalidImplementation = 417,
        InternalServerError = 500

        // Continue = 100,
        // SwitchingProtocols = 101,
        // Ok = 200,
        // Created = 201,
        // Accepted = 202,
        // NonAuthoritativeInformation = 203,
        // NoContent = 204,
        // ResetContent = 205,
        // PartialContent = 206,
        // Ambiguous = 300,
        // MultipleChoices = 300,
        // Moved = 301,
        // MovedPermanently = 301,
        // Found = 302,
        // Redirect = 302,
        // RedirectMethod = 303,
        // SeeOther = 303,
        // NotModified = 304,
        // UseProxy = 305,
        // Unused = 306,
        // RedirectKeepVerb = 307,
        // TemporaryRedirect = 307,
        // BadRequest = 400,
        // Unauthorized = 401,
        // PaymentRequired = 402,
        // Forbidden = 403,
        // NotFound = 404,
        // MethodNotAllowed = 405,
        // NotAcceptable = 406,
        // ProxyAuthenticationRequired = 407,
        // RequestTimeout = 408,
        // Conflict = 409,
        // Gone = 410,
        // LengthRequired = 411,
        // PreconditionFailed = 412,
        // RequestEntityTooLarge = 413,
        // RequestUriTooLong = 414,
        // UnsupportedMediaType = 415,
        // RequestedRangeNotSatisfiable = 416,
        // ExpectationFailed = 417,
        // UpgradeRequired = 426,
        // InternalServerError = 500,
        // NotImplemented = 501,
        // BadGateway = 502,
        // ServiceUnavailable = 503,
        // GatewayTimeout = 504,
        // HttpVersionNotSupported = 505,
    }
}
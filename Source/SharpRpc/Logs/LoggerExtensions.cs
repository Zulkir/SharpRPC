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

namespace SharpRpc.Logs
{
    public static class LoggerExtensions
    {
        public static void Info(this ILogger logger, string message)
        {
            logger.Custom(LogEntryType.Info, message);
        }

        public static void Info(this ILogger logger, Exception exception)
        {
            logger.Custom(LogEntryType.Info, null, exception);
        }

        public static void Info(this ILogger logger, string message, Exception exception)
        {
            logger.Custom(LogEntryType.Info, message, exception);
        }

        public static void Warning(this ILogger logger, string message)
        {
            logger.Custom(LogEntryType.Warning, message);
        }

        public static void Warning(this ILogger logger, Exception exception)
        {
            logger.Custom(LogEntryType.Warning, null, exception);
        }

        public static void Warning(this ILogger logger, string message, Exception exception)
        {
            logger.Custom(LogEntryType.Warning, message, exception);
        }

        public static void Error(this ILogger logger, string message)
        {
            logger.Custom(LogEntryType.Error, message);
        }

        public static void Error(this ILogger logger, Exception exception)
        {
            logger.Custom(LogEntryType.Error, null, exception);
        }

        public static void Error(this ILogger logger, string message, Exception exception)
        {
            logger.Custom(LogEntryType.Error, message, exception);
        }

        public static void Fatal(this ILogger logger, string message)
        {
            logger.Custom(LogEntryType.Fatal, message);
        }

        public static void Fatal(this ILogger logger, Exception exception)
        {
            logger.Custom(LogEntryType.Fatal, null, exception);
        }

        public static void Fatal(this ILogger logger, string message, Exception exception)
        {
            logger.Custom(LogEntryType.Fatal, message, exception);
        }

        public static void Debug(this ILogger logger, string message)
        {
            logger.Custom(LogEntryType.Debug, message);
        }

        public static void Debug(this ILogger logger, Exception exception)
        {
            logger.Custom(LogEntryType.Debug, null, exception);
        }

        public static void Debug(this ILogger logger, string message, Exception exception)
        {
            logger.Custom(LogEntryType.Debug, message, exception);
        }
    }
}
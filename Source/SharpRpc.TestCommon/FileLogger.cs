using System;
using System.IO;
using SharpRpc.Interaction;
using SharpRpc.Logs;

namespace SharpRpc.TestCommon
{
    public class FileLogger : ILogger, IDisposable
    {
        private readonly StreamWriter writer;
        private readonly DateTime start;
        private readonly object writeLock = new object();

        public FileLogger(string fileName)
        {
            writer = new StreamWriter(fileName);
            writer.AutoFlush = true;
            start = DateTime.Now;
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Custom(LogEntryType type, string message, Exception exception = null)
        {
            lock (writeLock)
            {
                writer.WriteLine("{0}: {1}", type.ToString().ToUpper(), message);
                WriteException(exception);
            }
        }

        public void NetworkingException(string message, Exception exception)
        {
            lock (writeLock)
            {
                writer.WriteLine(message);
                WriteException(exception);
            }
            
        }

        public void IncomingRequest(Request request)
        {
            lock (writeLock)
                writer.WriteLine("{0} Incoming: {1} for scope '{2}'", DateTime.Now - start, request.Path, request.ServiceScope);
        }

        public void ProcessNotReady(Request request)
        {
            lock (writeLock)
                writer.WriteLine("Not ready for request '{0}' for scope '{1}'", request.Path, request.ServiceScope);
        }

        public void ProcessedRequestSuccessfully(Request request, TimeSpan executionTime)
        {
            lock (writeLock)
                writer.WriteLine("{0} Success: {1} for scope '{2}' within {3} ms", DateTime.Now - start, request.Path, request.ServiceScope, Math.Round(executionTime.TotalMilliseconds));
        }

        public void ProcessedRequestWithBadStatus(Request request, ResponseStatus responseStatus)
        {
            lock (writeLock)
                writer.WriteLine("Error ({0}): {1} for scope '{2}'", responseStatus, request.Path, request.ServiceScope);
        }

        public void ProcessedRequestWithException(Request request, Exception exception)
        {
            lock (writeLock)
            {
                writer.WriteLine("Exception: {0} for scope '{1}'", request.Path, request.ServiceScope);
                WriteException(exception);
            }
        }

        private void WriteException(Exception exception)
        {
            lock (writeLock)
            {
                while (exception != null)
                {
                    writer.WriteLine(exception.Message);
                    writer.WriteLine(exception.StackTrace);
                    writer.WriteLine("--- end of stack trace ---");
                    exception = exception.InnerException;
                }
            }
        }
    }
}
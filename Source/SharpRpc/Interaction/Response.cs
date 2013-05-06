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

        public static Response NotReady { get { return new Response(ResponseStatus.NotReady, CommonImmuables.EmptyBytes); } }
        public static Response BadRequest { get { return new Response(ResponseStatus.BadRequest, CommonImmuables.EmptyBytes); } }
        public static Response NotFound { get { return new Response(ResponseStatus.ServiceNotFound, CommonImmuables.EmptyBytes); } }
        public static Response InvalidImplementation { get { return new Response(ResponseStatus.InvalidImplementation, CommonImmuables.EmptyBytes); } }
        public static Response InternalError { get { return new Response(ResponseStatus.InternalServerError, CommonImmuables.EmptyBytes); } }
    }
}

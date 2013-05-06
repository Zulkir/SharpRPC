namespace SharpRpc.TestCommon
{
    public class MyService : IMyService, IServiceImplementation
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public string Greet(string name)
        {
            return string.Format("Hello, {0}!", name);
        }

        public void Dispose()
        {
            
        }

        public ServiceImplementationState State { get; private set; }

        public void Initialize(string scope)
        {
            State = ServiceImplementationState.Running;
        }
    }
}
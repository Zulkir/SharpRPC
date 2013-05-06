using System;
using SharpRpc.TestCommon;

namespace SharpRpc.TestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var topology = new Topology();
            topology.AddEndPoint("MyService", null, new ServiceEndPoint("http", "localhost", 7001));
            var hostSettings = new ServiceHostSettings(new ServiceEndPoint("http", "localhost", 7001), 
                new[] {new InterfaceImplementationTypePair(typeof(IMyService), typeof(MyService))});
            var kernel = new RpcKernel(topology, hostSettings);

            kernel.StartHost();

            string line = Console.ReadLine();
            while (line != "exit")
            {
                line = Console.ReadLine();
            }

            kernel.StopHost();
        }
    }
}

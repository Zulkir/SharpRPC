using System;
using SharpRpc.TestCommon;

namespace SharpRpc.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var topology = new Topology();
            topology.AddEndPoint("MyService", null, new ServiceEndPoint("http", "localhost", 7001));
            var hostSettings = ServiceHostSettings.Empty;
            var kernel = new RpcKernel(topology, hostSettings);

            var client = kernel.GetService<IMyService>(null);

            string line;
            while ((line = Console.ReadLine()) != "exit")
            {
                switch (line)
                {
                    case "greet":
                    {
                        Console.Write("Enter a name: ");
                        var name = Console.ReadLine();
                        var greeting = client.Greet(name);
                        Console.WriteLine(greeting);
                    }
                    break;
                    case "add":
                    {
                        Console.Write("Enter a: ");
                        var a = int.Parse(Console.ReadLine());
                        Console.Write("Enter b: ");
                        var b = int.Parse(Console.ReadLine());
                        var sum = client.Add(a, b);
                        Console.WriteLine(sum);
                    }
                    break;
                    default:
                    {
                        Console.WriteLine("Unkown command");    
                    }
                    break;
                }
            }
        }
    }
}

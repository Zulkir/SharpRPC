#region License
/*
Copyright (c) 2013 Daniil Rodin of Buhgalteria.Kontur team of SKB Kontur

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
using SharpRpc.Logs;
using SharpRpc.Settings;
using SharpRpc.TestCommon;
using SharpRpc.Topology;

namespace SharpRpc.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var topology = new TopologyParser().Parse(@"MyService single http://localhost:7001");
            var hostSettings = HostSettings.Empty;
            var kernel = new RpcKernel(topology, hostSettings, new ConsoleLogger());

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

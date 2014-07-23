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
using System.Text;
using SharpRpc.Logs;
using SharpRpc.TestCommon;
using SharpRpc.Topology;

namespace SharpRpc.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var topologyLoader = new TopologyLoader("../Topology/topology.txt", Encoding.UTF8, new TopologyParser());
            var client = new RpcClient(topologyLoader, new TimeoutSettings(1, 100));

            var myService = client.GetService<IMyService>();

            var clientLog = new FileLogger("clientlog.txt");

            string line;
            while ((line = Console.ReadLine()) != "exit")
            {
                switch (line)
                {
                    case "greet":
                    {
                        Console.Write("Enter a name: ");
                        var name = Console.ReadLine();
                        var greeting = myService.Greet(name);
                        Console.WriteLine(greeting);
                        break;
                    }
                    case "add":
                    {
                        Console.Write("Enter a: ");
                        var a = int.Parse(Console.ReadLine());
                        Console.Write("Enter b: ");
                        var b = int.Parse(Console.ReadLine());
                        var sum = myService.Add(a, b);
                        Console.WriteLine(sum);
                        break;
                    }
                    case "aadd":
                    {
                        Console.Write("Enter a: ");
                        var a = int.Parse(Console.ReadLine());
                        Console.Write("Enter b: ");
                        var b = int.Parse(Console.ReadLine());
                        var sum = myService.AddAsync(a, b).Result;
                        Console.WriteLine(sum);
                        break;
                    }
                    case "throw":
                    {
                        try
                        {
                            myService.Throw();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        break;
                    }
                    case "sleep":
                    {
                        try
                        {
                            myService.SleepOneSecond();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        break;
                    }
                    case "stress":
                    {
                        var globalStart = DateTime.Now;
                        for (int i = 0; i < 10000; i++)
                        {
                            int iLoc = i;
                            var start = DateTime.Now;
                            clientLog.Info(string.Format("{0}:\t{1} start", iLoc, DateTime.Now - globalStart));
                            myService.AddAsync(i, 2 * i).ContinueWith(t => clientLog.Info(string.Format("{0}:\t{1}\t{2}", iLoc, DateTime.Now - globalStart, DateTime.Now - start)));
                        }
                        break;
                    }
                    default:
                    {
                        Console.WriteLine("Unkown command");
                        break;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Interfaces;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Polly;

namespace Client
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await RunMainAsync();
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = StartClient())
                {
                    //Console.WriteLine($"client is Initialised: {client.IsInitialized}");
                    //var key = Guid.NewGuid();
           
                    //var helloGrain = client.GetGrain<IHello>(Guid.NewGuid());
                    //var helloGrain2 = client.GetGrain<IHello>(key);
                    //var helloGrain3 = client.GetGrain<IHello>(key);
                    //var response = await helloGrain.SayHello("Good Morning");
                    //var response2 = await helloGrain2.SayHello("Good Afternoon");
                    //var response3 = await helloGrain3.SayHello("Good Evening");
                    //Console.WriteLine(response);
                    //Console.WriteLine(response2);
                    //Console.WriteLine(response3);

                    var greetingGrain = client.GetGrain<IGreetingGrain>(0);
                var response=  await  greetingGrain.SendGreeting("Hello");

                    var greetingGrain1 = client.GetGrain<IGreetingGrain>(0);
                    await greetingGrain1.SendGreeting("Morning");

                    var greetingGrain2 = client.GetGrain<IGreetingGrain>(0);
                    await greetingGrain2.SendGreeting("Afternoon");


                    Console.ReadLine();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return -1;
            }
        }

        private static IClusterClient StartClient()
        {
            return Policy<IClusterClient>
                .Handle<SiloUnavailableException>()
                .Or<OrleansMessageRejectionException>()
                .WaitAndRetry(
                    new List<TimeSpan>
                    {
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(4)
                    }).Execute(() =>
                {
                    var client = new ClientBuilder()
                        //clustering inforation
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "HelloApp";
                        })
                        //Clustering provider
                        .UseLocalhostClustering().Build();

                    client.Connect().GetAwaiter().GetResult();
                    Console.WriteLine("client connected");
                    return client;
                });
        }
    }
}
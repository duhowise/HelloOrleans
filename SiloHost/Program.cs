using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Grains;
using Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Logging;
using SiloHost.Filters;

namespace SiloHost
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            return await RunSilo();
        }

        private static async Task<int> RunSilo()
        {
            try
            {
                await StartSilo();
                Console.WriteLine("silo started");
                Console.WriteLine("press enter to terminate");
                Console.ReadLine();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            var config = LoadConfig();
            var orleansConfig = GetOrleansConfig(config);
            var builder = new SiloHostBuilder()
                    //cluster information
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "HelloApp";
                    }).UseLocalhostClustering()
                    //EndPoint information
                    .Configure<EndpointOptions>(option =>
                    {
                        option.SiloPort = 11111;
                        option.GatewayPort = 30000;
                        option.AdvertisedIPAddress = IPAddress.Loopback;
                    }).UseDashboard()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(s => CreateGrainMethodList());
                        services.AddSingleton(s => new JsonSerializerSettings
                        {
                            Formatting = Formatting.None,
                            TypeNameHandling = TypeNameHandling.None,
                            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                        });

                    })
                    .AddIncomingGrainCallFilter<LoggingFilter>()
                    .AddAdoNetGrainStorageAsDefault(options =>
                    {
                        options.Invariant = orleansConfig.Invariant;
                        options.ConnectionString = orleansConfig.ConnectionString;
                        options.UseJsonFormat = orleansConfig.UseJsonFormat;
                    })
                    .ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(HelloGrain).Assembly).WithReferences())
                    .ConfigureLogging(logging => logging.AddFile("OrleansLog.txt"))
                ;


            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        private static IConfigurationRoot LoadConfig()
        {
            var builder = new ConfigurationBuilder();
            // ReSharper disable once StringLiteralTypo
            builder.AddJsonFile("appsettings.json");
            var config = builder.Build();
            return config;
        }

        private static GrainInfo CreateGrainMethodList()
        {
            var grainInterfaces = typeof(IHello).Assembly.GetTypes().Where(x => x.IsInterface)
                .SelectMany(type => type.GetMethods()).Select(methodInfo => methodInfo.Name).Distinct();

            return new GrainInfo
            {
                Methods = grainInterfaces.ToList()
            };
        }

        private static OrleansConfig GetOrleansConfig(IConfiguration configuration)
        {
            var orleansConfig = new OrleansConfig();
            var section = configuration.GetSection("OrleansConfiguration");
            section.Bind(orleansConfig);
            return orleansConfig;
        }
    }

    public class GrainInfo
    {
        public GrainInfo()
        {
            Methods = new List<string>();
        }

        public List<string> Methods { get; set; }
    }
}
using System;
using System.Net;
using System.Threading.Tasks;
using Grains;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Logging;

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
         var config=   LoadConfig();
       var orleansConfig=  GetOrleansConfig(config);
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
                    .AddAdoNetGrainStorageAsDefault(options =>
                    {
                        options.Invariant = orleansConfig.Invariant;
                        options.ConnectionString =orleansConfig.ConnectionString;
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

        private static OrleansConfig GetOrleansConfig(IConfiguration configuration)
        {
            var orleansConfig=new OrleansConfig();
            var section = configuration.GetSection("OrleansConfiguration");
            section.Bind(orleansConfig);
            return orleansConfig;
        }

    }

    public class OrleansConfig
    {
        public string ConnectionString { get; set; }
        public string Invariant { get; set; }
        public bool UseJsonFormat { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Grains;
using Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Logging;
using SiloHost.Context;
using SiloHost.Filters;

namespace SiloHost
{
    internal class Program
    {
        private static readonly ManualResetEvent SiloStopped = new ManualResetEvent(false);
        private static bool _siloStopping=false;
        private static readonly object SynLock = new object();
        private static ISiloHost _silo;

        private static async Task<int> Main(string[] args)
        {
            SetupApplicationShutdown();

            return await RunSilo();
        }

        private static async Task<int> RunSilo()
        {
            try
            {
                _silo = await StartSilo();
                Console.WriteLine("silo started");
                Console.WriteLine("press enter to terminate");
                SiloStopped.WaitOne();
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
                    })
                    .UseDashboard()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IOrleansRequestContext, OrleansRequestContext>();
                        services.AddSingleton(s => CreateGrainMethodList());
                        services.AddSingleton(s => new JsonSerializerSettings
                        {
                            Formatting = Formatting.None,
                            TypeNameHandling = TypeNameHandling.None,
                            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                        });

                        services.AddSingleton(x => CreateEventStoreConnection());
                    }).AddCustomStorageBasedLogConsistencyProvider("CustomStorage")
                    .AddIncomingGrainCallFilter<LoggingFilter>()
                    .AddAdoNetGrainStorageAsDefault(options =>
                    {
                        options.Invariant = orleansConfig.Invariant;
                        options.ConnectionString = orleansConfig.ConnectionString;
                        options.UseJsonFormat = orleansConfig.UseJsonFormat;
                    })
                    .ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(HelloGrain).Assembly).WithReferences())
                    .ConfigureLogging(logging =>
                    {
                        logging.AddFile("OrleansLog.txt");
                        logging.AddDebug();
                        logging.AddConsole();
                    })
                ;


            _silo = builder.Build();
            await _silo.StartAsync();
            return _silo;
        }

        private static IEventStoreConnection CreateEventStoreConnection()
        {
            var connectionString = "ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=500";

            var connection = EventStoreConnection.Create(connectionString);
            connection.ConnectAsync().GetAwaiter().GetResult();
            return connection;
        }

        static void SetupApplicationShutdown()
        {
            Console.CancelKeyPress += (s, a) =>
            {
                a.Cancel = true;
                lock (SynLock)
                {
                    if (!_siloStopping)
                    {
                        _siloStopping = true;
                        Task.Run(StopSilo).Ignore();
                    }
                }
            };
        }

        static async Task StopSilo()
        {
            await _silo.StopAsync();
            SiloStopped.Set();
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
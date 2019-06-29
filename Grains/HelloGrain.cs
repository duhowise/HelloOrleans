using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Grains
{
    [StorageProvider]
    public class HelloGrain : Grain<GreetingArchive>, IHello
    {
        private readonly ILogger<HelloGrain> _logger;

        public HelloGrain(ILogger<HelloGrain> logger)
        {
            _logger = logger;
        }

        public async Task<string> SayHello(string greeting)
        {
            State.Greetings.Add(greeting);
            await WriteStateAsync();
            var primaryKey = this.GetPrimaryKey();
            Console.WriteLine($"this is primary key : {primaryKey}");
            
            return await Task.FromResult($"you said :{greeting} i say hello ");
        }
    }


    public class GreetingArchive
    {
        public List<string> Greetings { get; } = new List<string>();
    }
}
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Interfaces;
using Newtonsoft.Json;
using Orleans;
using Orleans.EventSourcing;
using Orleans.EventSourcing.StateStorage;
using Orleans.Providers;

namespace Grains
{
   [LogConsistencyProvider(ProviderName = "CustomStorage")] public class GreetingsGrain:EventSourcedGrain<GreetingState,GreetingEvent>,IGreetingGrain

    {
        public async Task<string> SendGreeting(string greetings)
        {
            var state = State.Greeting;
            RaiseEvent(new GreetingEvent{Greeting = greetings});
            await ConfirmEvents();
            return greetings;
        }

        public GreetingsGrain(IEventStoreConnection eventStoreConnection, JsonSerializerSettings jsonSerializerSettings) : base(eventStoreConnection, jsonSerializerSettings)
        {
        }

        protected override string GetGrainKey()
        {
            return this.GetPrimaryKeyLong().ToString();
        }
    }

    public class GreetingEvent
    {
        public string Greeting { get; set; }
    }

    public class GreetingState
    {
        public string Greeting { get; set; }

        public GreetingState Apply(GreetingEvent @event)
        {
            Greeting = @event.Greeting;
            return this;
        }
    }
}
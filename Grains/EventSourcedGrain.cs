using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.EventSourcing;
using Orleans.EventSourcing.CustomStorage;

namespace Grains
{
    public abstract class EventSourcedGrain<TGrainState, TEventBase>
      : JournaledGrain<TGrainState, TEventBase>,
          ICustomStorageInterface<TGrainState, TEventBase>
      where TGrainState : class, new()
      where TEventBase : class

    {
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        protected abstract string GetGrainKey();


        protected EventSourcedGrain(IEventStoreConnection eventStoreConnection,
                                    JsonSerializerSettings jsonSerializerSettings)
        {
            _eventStoreConnection = eventStoreConnection;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        protected new async Task<IEnumerable<TEventBase>> RetrieveConfirmedEvents(int fromVersion, int toVersion)
        {

            var resolvedEvents = new List<TEventBase>();

            var allEventsSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(GetStreamName(), fromVersion, toVersion, false);
            foreach (var resolvedEvent in allEventsSlice.Events)
                resolvedEvents.Add(DeserializeEvent(resolvedEvent.Event) as TEventBase);

            return resolvedEvents;

        }

        public virtual async Task<KeyValuePair<int, TGrainState>> ReadStateFromStorage()
        {
            var version = 0;
            var state = new TGrainState();

            try
            {
                var allEventsSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(GetStreamName(), 0L, 100, false);

                foreach (var resolvedEvent in allEventsSlice.Events)
                {
                    version = (int)resolvedEvent.OriginalEventNumber;
                    var delta = DeserializeEvent(resolvedEvent.Event);
                    var deltaType = Type.GetType(GetEventClrTypeName(resolvedEvent.Event));
                    var methodInfo = typeof(TGrainState).GetMethod("Apply", new[] { deltaType });
                    methodInfo?.Invoke(state, new[] { delta });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return new KeyValuePair<int, TGrainState>(version, state);
        }

        public virtual async Task<bool> ApplyUpdatesToStorage(IReadOnlyList<TEventBase> updates, int expectedversion)
        {
            var version = GetProviderVersion(expectedversion);

            if (string.IsNullOrEmpty(GetGrainKey()))
                throw new ArgumentNullException(GetGrainKey());

            foreach (var update in updates)
            {
                try
                {

                    var eventData = ToEventData(update, new Dictionary<string, object>());
                    await _eventStoreConnection.AppendToStreamAsync(GetStreamName(), version, eventData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }

            return true;
        }

        private static int GetProviderVersion(int expectedVersion)
        {
            return expectedVersion == 0 ? ExpectedVersion.NoStream : expectedVersion - 1;
        }

        private EventData ToEventData(object @event, IDictionary<string, object> headers)
        {
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, _jsonSerializerSettings));

            var eventHeaders = new Dictionary<string, object>(headers)
            {
                {
                    "EventClrType", @event.GetType().AssemblyQualifiedName
                }
            };
            var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeaders));
            var typeName = @event.GetType().Name;

            return new EventData(Guid.NewGuid(), typeName, true, data, metadata);
        }

        private static object DeserializeEvent(RecordedEvent eventData)
        {
            var eventClrTypeName = GetEventClrTypeName(eventData);
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(eventData.Data), Type.GetType(eventClrTypeName));
        }

        private static string GetEventClrTypeName(RecordedEvent eventData)
        {
            var eventClrTypeName = JObject.Parse(Encoding.UTF8.GetString(eventData.Metadata)).Property("EventClrType").Value;
            return eventClrTypeName.ToString();
        }

        private string GetStreamName()
        {
            var grainKey = GetGrainKey();
            var stream = "OurGreetingsStream:{0}:{1}"
                .Replace("{0}", GetType().Name)
                .Replace("{1}", grainKey);
            return stream;
        }
    }
}
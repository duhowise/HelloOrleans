using System.Threading.Tasks;
using Orleans;

namespace Interfaces
{
    public interface IGreetingGrain : IGrainWithIntegerKey
    {
        Task<string> SendGreeting(string greetings);
    }
}
using System;

namespace SiloHost.Context
{
    public interface IOrleansRequestContext
    {
        Guid TraceId { get; }
    }
}
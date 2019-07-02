using System;

namespace SiloHost1.Context
{
    public interface IOrleansRequestContext
    {
        Guid TraceId { get; }
    }
}
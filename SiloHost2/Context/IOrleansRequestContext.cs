using System;

namespace SiloHost2.Context
{
    public interface IOrleansRequestContext
    {
        Guid TraceId { get; }
    }
}
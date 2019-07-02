using System;
using Orleans.Runtime;

namespace SiloHost2.Context
{

    public class OrleansRequestContext : IOrleansRequestContext
    {
        public Guid TraceId =>
            RequestContext.Get("traceId") == null ? Guid.Empty : (Guid) RequestContext.Get("traceId");
    }
}
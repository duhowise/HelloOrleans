using System;
using Orleans.Runtime;

namespace SiloHost.Context
{

    public class OrleansRequestContext : IOrleansRequestContext
    {
        public Guid TraceId =>
            RequestContext.Get("traceId") == null ? Guid.Empty : (Guid) RequestContext.Get("traceId");
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using SiloHost2.Context;

namespace SiloHost2.Filters
{
    public class LoggingFilter : IIncomingGrainCallFilter
    {
        private readonly GrainInfo _grainInfo;
        private readonly ILogger<LoggingFilter> _logger;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IOrleansRequestContext _context;

        public LoggingFilter(GrainInfo grainInfo, ILogger<LoggingFilter> logger,
            JsonSerializerSettings jsonSerializerSettings,IOrleansRequestContext context)
        {
            _grainInfo = grainInfo;
            _logger = logger;
            _jsonSerializerSettings = jsonSerializerSettings;
            _context = context;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            try
            {
                if (ShouldLog(context.InterfaceMethod.Name))
                {
                    var arguments = JsonConvert.SerializeObject(context.Arguments, _jsonSerializerSettings);
                    _logger.LogInformation($"LOGGING_FILTER TraceId: {_context.TraceId} {context.Grain.GetType()}.{context.InterfaceMethod.Name}: arguments: {arguments} request");
                }

                await context.Invoke();
                if (ShouldLog(context.InterfaceMethod.Name))
                {
                    var result = JsonConvert.SerializeObject(context.Result, _jsonSerializerSettings);
                    _logger.LogInformation($"LOGGING_FILTER  TraceId: {_context.TraceId} {context.Grain.GetType()}.{context.InterfaceMethod.Name}: result: {result} request");

                }
            }
            catch (Exception e)
            {
                var arguments = JsonConvert.SerializeObject(context.Arguments, _jsonSerializerSettings);
                var result = JsonConvert.SerializeObject(context.Result, _jsonSerializerSettings);

                _logger.LogError($"LOGGING_FILTER TraceId: {_context.TraceId} {context.Grain.GetType()}.{context.InterfaceMethod.Name}: threw an exception: {nameof(e)} request",e);

                throw;
            }
        }


        private bool ShouldLog(string methodName)
        {
            return _grainInfo.Methods.Contains(methodName);
        }
    }
}
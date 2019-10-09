using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

[assembly: HostingStartup(typeof(Api1.LoggingHostingStartup))]

namespace Api1
{
    internal class LoggingStartupFilter : IStartupFilter
    {
        public LoggingStartupFilter(IOptionsMonitor<LoggerSourceContextLevelOverrides> monitor)
        {
            _ = monitor.CurrentValue;
        }
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
            };
        }

    }
}

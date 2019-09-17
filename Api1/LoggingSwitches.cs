using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;


[assembly: HostingStartup(typeof(Api1.LoggingHostingStartup))]

namespace Api1
{
    class LoggingSwitches
    {
        public static void ConfigureOptions(LoggingSwitches options, ConcurrentDictionary<string, LogEventLevel> switches, IConfiguration configuration, ILogger<LoggingSwitches> logger)
        {
            var dictionary = new Dictionary<string, LogEventLevel>();
            configuration.GetSection(nameof(LoggingSwitches)).Bind(dictionary);

            foreach (var item in dictionary)
            {
                switches.AddOrUpdate(item.Key, _ => item.Value, (_, __) => item.Value);
            }

            logger.LogInformation("Options changed to {@LoggingSwitchOptions}", switches);

        }
    }
}

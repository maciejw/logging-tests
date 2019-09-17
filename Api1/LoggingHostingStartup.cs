using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;


[assembly: HostingStartup(typeof(Api1.LoggingHostingStartup))]

namespace Api1
{
    class LoggingHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            var CategoryFilters = new ConcurrentDictionary<string, LogEventLevel>();

            void ConfigureLogger(WebHostBuilderContext context, LoggerConfiguration loggerConfiguration)
            {
                var auditLogger = new LoggerConfiguration()
                    .Filter.ByIncludingOnly(Matching.FromSource<Audit>())
                    .AuditTo.File(new CompactJsonFormatter(), "audit-logger.json")
                    .CreateLogger();

                bool CategoriesBelowCertainLevel(LogEvent logEvent) =>
                        CategoryFilters.Keys.Any(sourceContext =>
                            Matching.FromSource(sourceContext).Invoke(logEvent)
                            && CategoryFilters.TryGetValue(sourceContext, out var level)
                            && logEvent.Level < level);

                var eventLogger = new LoggerConfiguration()
                    .Filter.ByExcluding(CategoriesBelowCertainLevel)
                    .WriteTo.File(new CompactJsonFormatter(), "event-logger.json", shared: true)
                    .CreateLogger();

                loggerConfiguration
                    .MinimumLevel.Verbose()
                    .Enrich.FromLogContext()
                    .AuditTo.Logger(auditLogger)
                    .WriteTo.Logger(eventLogger)
                    .WriteTo.Console(
                        restrictedToMinimumLevel: LogEventLevel.Information,
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Properties:j}{NewLine}{Exception}",
                        theme: AnsiConsoleTheme.Code
                    )
                    .WriteTo.Seq(serverUrl: "http://localhost:5341",
                                 restrictedToMinimumLevel: LogEventLevel.Verbose,
                                 compact: true);

            }

            void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
            {
                services.AddSingleton(CategoryFilters);
                
                services.AddSingleton<IStartupFilter, LoggingStartupFilter>();

                services.AddOptions<LoggingSwitches>()
                    .Bind(context.Configuration)
                    .Configure<ConcurrentDictionary<string, LogEventLevel>, IConfiguration, ILogger<LoggingSwitches>>(LoggingSwitches.ConfigureOptions);

            }

            builder.ConfigureServices(ConfigureServices);
            builder.UseSerilog(ConfigureLogger);
        }
    }
}

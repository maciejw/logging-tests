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
    public class ConfigureSerilog
    {
        private ConcurrentDictionary<string, LogEventLevel> CategoryFilters = new ConcurrentDictionary<string, LogEventLevel>();

        public void ConfigureLogging(LoggerConfiguration loggerConfiguration)
        {
            CompactJsonFormatter fileFormatter = new CompactJsonFormatter();

            var auditLogger = new LoggerConfiguration()
                .Filter.ByIncludingOnly(Matching.FromSource<Audit>())
                .AuditTo.File(fileFormatter, "audit-logger.json")
                .CreateLogger();

            var eventLogger = new LoggerConfiguration()
                .Filter.ByExcluding(CategoriesBelowCertainLevel)
                .WriteTo.File(fileFormatter, "event-logger.json", shared: true)
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
        public void ConfigureLoggingServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton(CategoryFilters);

            services.AddOptions<LoggingSwitches>()
                .Bind(configuration)
                .Configure<ConcurrentDictionary<string, LogEventLevel>, IConfiguration, ILogger<LoggingSwitches>>(LoggingSwitches.ConfigureOptions);

        }

        private bool CategoriesBelowCertainLevel(LogEvent logEvent)
        {
            return CategoryFilters.Keys.Any(sourceContext =>
                Matching.FromSource(sourceContext).Invoke(logEvent)
                && CategoryFilters.TryGetValue(sourceContext, out var level)
                && logEvent.Level < level);
        }
    }

    class LoggingHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            var configureSerilog = new ConfigureSerilog();

            builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IStartupFilter, LoggingStartupFilter>();
                configureSerilog.ConfigureLoggingServices(context.Configuration, services);
            });
            builder.UseSerilog((_, loggerConfiguration) => configureSerilog.ConfigureLogging(loggerConfiguration));
        }
    }
}

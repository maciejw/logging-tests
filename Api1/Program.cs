using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;


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

    class LoggingStartupFilter : IStartupFilter
    {
        public LoggingStartupFilter(IOptionsMonitor<LoggingSwitches> options)
        {
            var _ = options.CurrentValue;
        }
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
            };
        }

    }

    interface Audit { }

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
        }
    }
}

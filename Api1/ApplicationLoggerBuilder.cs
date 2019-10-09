using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Formatting.Json;
#if NETCOREAPP2_2
using HostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif
#if NETCOREAPP3_0
using HostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

namespace Api1
{
    public class ApplicationLoggerBuilder
    {
        private readonly LoggerSourceContextLevelOverrides globalSwitches;

        public ApplicationLoggerBuilder()
        {
            globalSwitches = new LoggerSourceContextLevelOverrides();
        }

        public void ConfigureLogging(LoggerConfiguration loggerConfiguration, HostingEnvironment hostingEnvironment)
        {
            var fileFormatter = new CompactJsonFormatter();

            //Serilog.Core.Logger auditLogger = new LoggerConfiguration()
            //    .Filter.ByIncludingOnly(Matching.FromSource<Audit>())
            //    .AuditTo.File(fileFormatter, "audit-logger.json")
            //    .CreateLogger();

            Serilog.Core.Logger applicationLogger = new LoggerConfiguration()
                .Filter().Overrides(globalSwitches)
                .WriteTo.File(fileFormatter, "application-logger.json", shared: true)
                .CreateLogger();

            loggerConfiguration
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                //.AuditTo.Logger(auditLogger)
                .WriteTo.Logger(applicationLogger)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}",
                    theme: GetTheme(hostingEnvironment)
                )
                .WriteTo.Seq(serverUrl: "http://localhost:5341", compact: true);

        }

        private static ConsoleTheme GetTheme(HostingEnvironment hostingEnvironment)
        {
            return hostingEnvironment.IsEnvironment("Testing") ? ConsoleTheme.None : AnsiConsoleTheme.Code;
        }
        public void ConfigureLoggingServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton(globalSwitches);

            services.WatchOptions<LoggerSourceContextLevelOverrides>(configuration);

            services.ConfigureOptions<LoggingSwitchesConfiguration>();

        }
    }
}

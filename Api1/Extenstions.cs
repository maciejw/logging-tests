using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;


namespace Api1
{
    public static class Extenstions
    {

        public static IServiceCollection WatchOptions<T>(this IServiceCollection @this, IConfiguration configuration)
        {
            ConfigurationChangeTokenSource<T> changeTokenSource = new ConfigurationChangeTokenSource<T>(configuration);
            return @this.AddSingleton<IOptionsChangeTokenSource<T>>(changeTokenSource);
        }
        public static IWebHostBuilder UseApplicationLogger(this IWebHostBuilder @this)
        {
            ApplicationLoggerBuilder applicationLoggerBuilder = new ApplicationLoggerBuilder();

            @this.ConfigureServices((context, services) =>
            {
                services.AddSingleton<IStartupFilter, LoggingStartupFilter>();
                applicationLoggerBuilder.ConfigureLoggingServices(context.Configuration, services);
            });

            return @this.UseSerilog((context, loggerConfiguration) =>
            {
                applicationLoggerBuilder.ConfigureLogging(loggerConfiguration, context.HostingEnvironment);
            });
        }
    }
}

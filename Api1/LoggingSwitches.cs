using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api1
{

    public class LoggingSwitchesConfiguration : IConfigureOptions<LoggerSourceContextLevelOverrides>
    {
        private readonly LoggerSourceContextLevelOverrides globalSwitches;
        private readonly IConfiguration configuration;
        private readonly ILogger<LoggerSourceContextLevelOverrides> logger;

        public LoggingSwitchesConfiguration(LoggerSourceContextLevelOverrides globalSwitches, IConfiguration configuration, ILogger<LoggerSourceContextLevelOverrides> logger)
        {
            this.globalSwitches = globalSwitches;
            this.configuration = configuration;
            this.logger = logger;
        }


        public void Configure(LoggerSourceContextLevelOverrides options)
        {
            configuration.GetSection(nameof(LoggerSourceContextLevelOverrides)).Bind(options, binder => binder.BindNonPublicProperties = true);

            globalSwitches.Update(options);

            logger.LogInformation("Options changed to {@LoggingSwitchOptions}", options);
        }
    }
}

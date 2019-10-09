using Serilog;
using Serilog.Events;

namespace Api1
{
    public class LoggerFilterConfiguration
    {
        private readonly LoggerConfiguration configuration;

        public LoggerFilterConfiguration(LoggerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public LoggerConfiguration MinimumLevel(LogEventLevel minimumLevel = LogEventLevel.Verbose)
        {
            configuration.MinimumLevel.Is(minimumLevel);

            return configuration;
        }

        public LoggerConfiguration Overrides(LoggerSourceContextLevelOverrides switches)
        {
            configuration.Filter.ByExcluding(e => EventsBelowCertainLevel(e, switches));
            return configuration;
        }

        public static bool EventsBelowCertainLevel(LogEvent logEvent, LoggerSourceContextLevelOverrides globalSwitches)
        {
            (LogEventLevel defaultLevel, System.Collections.Generic.KeyValuePair<System.Func<LogEvent, bool>, LogEventLevel>[] matchers) = globalSwitches.Current;

            for (int i = 0; i < matchers.Length; i++)
            {
                System.Collections.Generic.KeyValuePair<System.Func<LogEvent, bool>, LogEventLevel> filter = matchers[i];
                if (filter.Key(logEvent))
                {
                    return logEvent.Level < filter.Value;
                }
            }

            return logEvent.Level < defaultLevel;
        }
    }
}

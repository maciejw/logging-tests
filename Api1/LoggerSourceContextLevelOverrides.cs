using System;
using System.Collections.Generic;
using System.Threading;
using Serilog.Events;

namespace Api1
{
    public class LoggerSourceContextLevelOverrides
    {
        public class Configuration
        {
            private readonly LogEventLevel defaultLevel;
            private readonly KeyValuePair<Func<LogEvent, bool>, LogEventLevel>[] matchers;

            public Configuration(LogEventLevel defaultLevel, KeyValuePair<Func<LogEvent, bool>, LogEventLevel>[] matchers)
            {
                this.defaultLevel = defaultLevel;
                this.matchers = matchers;
            }
            public void Deconstruct(out LogEventLevel defaultLevel, out KeyValuePair<Func<LogEvent, bool>, LogEventLevel>[] matchers)
            {
                defaultLevel = this.defaultLevel;
                matchers = this.matchers;
            }
        }

        public LoggerSourceContextLevelOverrides()
        {

        }
        public LoggerSourceContextLevelOverrides(LogEventLevel defaultLevel = LogEventLevel.Information, params KeyValuePair<string, LogEventLevel>[] sourceContextFilters)
        {
            currentConfiguration = new Configuration(defaultLevel, new LoggerSourceContextLevelOverridesList(sourceContextFilters).GetMatchers());
        }
        private volatile Configuration currentConfiguration;

        public Configuration Current => currentConfiguration;

        public void Update(LoggerSourceContextLevelOverrides newSwitches)
        {
            Interlocked.Exchange(ref currentConfiguration, newSwitches.currentConfiguration);
        }


    }
}

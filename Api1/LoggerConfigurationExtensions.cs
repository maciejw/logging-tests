using Serilog;

namespace Api1
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerFilterConfiguration Filter(this LoggerConfiguration @this)
        {
            return new LoggerFilterConfiguration(@this);
        }
    }
}

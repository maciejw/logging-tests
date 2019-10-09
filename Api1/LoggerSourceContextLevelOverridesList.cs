using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Events;
using Serilog.Filters;

namespace Api1
{
    public class LoggerSourceContextLevelOverridesList : SortedList<string, LogEventLevel>
    {
        private static readonly Comparer<string> descendingComparer = Comparer<string>.Create((x, y) => StringComparer.InvariantCultureIgnoreCase.Compare(y, x));
        public LoggerSourceContextLevelOverridesList(params KeyValuePair<string, LogEventLevel>[] sourceContextFilters) : base(new Dictionary<string, LogEventLevel>(sourceContextFilters), descendingComparer) { }
        public KeyValuePair<Func<LogEvent, bool>, LogEventLevel>[] GetMatchers()
        {
            return this.Select(p => KeyValuePair.Create(Matching.FromSource(p.Key), p.Value)).ToArray();
        }
    }
}

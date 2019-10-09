using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Api1
{
    public class DiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly DiagnosticListener diagnosticListener;
        private readonly ILogger<DiagnosticListenerObserver> logger;
        private IDisposable subscription;

        public DiagnosticListenerObserver(DiagnosticListener diagnosticListener, ILogger<DiagnosticListenerObserver> logger)
        {
            this.diagnosticListener = diagnosticListener;
            this.logger = logger;
        }

        public void Subscribe(params string[] activityPrefixes)
        {
            IEnumerable<string> prefixes = new[] { "Microsoft.AspNetCore.Hosting.HttpRequestIn" }.Concat(activityPrefixes);

            bool isEnabled(string activity)
            {
                return prefixes.Any(prefix => activity.StartsWith(prefix));
            }

            subscription = diagnosticListener.Subscribe(this, isEnabled);
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        public void Unsubscribe()
        {
            subscription?.Dispose();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            logger.LogInformation(error, "Diagnostics Error");

        }

        public void OnNext(KeyValuePair<string, object> pair)
        {
            string baggage = string.Join(",", Activity.Current.Baggage?.Select(p => $"{p}"));
            (string key, object _) = pair;
            logger.LogInformation("Diagnostics {key}: {baggage}", key, baggage);
        }
    }

}

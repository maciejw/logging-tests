using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<DiagnosticListenerObserver>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {

            serviceProvider.GetRequiredService<DiagnosticListenerObserver>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
    class DiagnosticListenerObserver : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly DiagnosticListener diagnosticListener;
        private readonly ILogger<DiagnosticListenerObserver> logger;
        private readonly IDisposable subscription;

        public DiagnosticListenerObserver(DiagnosticListener diagnosticListener, ILogger<DiagnosticListenerObserver> logger)
        {
            bool isEnabled(string activity)
            {
                return activity.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            }

            subscription = diagnosticListener.Subscribe(this, isEnabled);
            this.diagnosticListener = diagnosticListener;
            this.logger = logger;
        }

        public void Dispose()
        {
            subscription.Dispose();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            logger.LogInformation("Diagnostics {key}: {@activity}", value.Key, Activity.Current);
        }
    }
  
}

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if NETCOREAPP2_2
using HostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif
#if NETCOREAPP3_0
using HostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

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

        public void Configure(IApplicationBuilder app, HostingEnvironment env, DiagnosticListenerObserver observer)
        {
            if (env.IsDevelopment() || env.IsEnvironment("Testing"))
            {
                observer.Subscribe();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.Use(next =>
            {
                return async context =>
                {
                    await next(context);
                };

            });
#if NETCOREAPP2_2
            app.UseMvc();
#endif
#if NETCOREAPP3_0

            app.UseRouting();

            app.UseEndpoints(routing =>
            {
                routing.MapControllers();
            });
#endif

        }
    }

}

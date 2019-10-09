using Microsoft.AspNetCore.Hosting;


[assembly: HostingStartup(typeof(Api1.LoggingHostingStartup))]

namespace Api1
{
    internal class LoggingHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseApplicationLogger();
        }
    }
}

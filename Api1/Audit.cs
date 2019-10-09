using Microsoft.AspNetCore.Hosting;


[assembly: HostingStartup(typeof(Api1.LoggingHostingStartup))]

namespace Api1
{
    internal interface Audit { }
}

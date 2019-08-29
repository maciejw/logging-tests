using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Test1
{

    public class MyHttpClient
    {
        public MyHttpClient(HttpClient client)
        {
            Client = client;
        }
        public HttpClient Client { get; set; }
    }
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddHttpClient<MyHttpClient>();

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                MyHttpClient myHttpClient = serviceProvider.GetService<MyHttpClient>();


                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5000/WeatherForecast");
                httpRequestMessage.Headers.Add("Correlation-Context", "correlation-id=1234,some-other-data=bla");
                httpRequestMessage.Headers.Add("Request-Id", "request-id");
                var response = await myHttpClient.Client.SendAsync(httpRequestMessage);



                Assert.True(response.IsSuccessStatusCode);

            }
        }


        [Fact]
        public void MyTestMethod()
        {

            var category1Switch = new LoggingLevelSwitch();

            bool CategoriesBelowCertainLevel(LogEvent e) => Matching.FromSource("Category1").Invoke(e) && e.Level < category1Switch.MinimumLevel;

            var logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Logger(configuration => configuration
                  .Filter.ByExcluding(CategoriesBelowCertainLevel)
                  .WriteTo.File("file1.log"))
              .WriteTo.Console()
              .CreateLogger();

            var category1Logger = logger.ForContext(Constants.SourceContextPropertyName, "Category1");
            var category2Logger = logger.ForContext(Constants.SourceContextPropertyName, "Category2");

            category1Logger.Information("visible");
            category2Logger.Information("visible");

            category1Switch.MinimumLevel = LogEventLevel.Error;

            category1Logger.Information("invisible");
            category2Logger.Information("visible");

        }
    }
}

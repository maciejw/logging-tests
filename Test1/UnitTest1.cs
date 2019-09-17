using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Test1
{


    public class MyHttpClientOptions
    {
        public string BaseAddress { get; set; }

    }

    public interface IMyHttpClient
    {
        HttpClient Client { get; }
    }
    public class MyHttpClient : IMyHttpClient
    {
        public MyHttpClient(IOptions<MyHttpClientOptions> options, HttpClient client)
        {
            Client = client;

            client.BaseAddress = new Uri(options.Value.BaseAddress);
        }
        public HttpClient Client { get; set; }
    }

    public class TestOptions
    {
        public bool CallTestService { get; set; } = true;
        public bool LogToConsole { get; set; } = false;
        public bool LogToTestOutput { get; set; } = true;

    }

    public class UnitTest1 : IClassFixture<WebApplicationFactory<Api1.Startup>>, IDisposable
    {
        private readonly WebApplicationFactory<Api1.Startup> webApplicationFactory;
        private readonly WebApplicationFactory<Api1.Startup> rootWwebApplicationFactory;
        private readonly ITestOutputHelper output;
        private readonly TestOptions options;

        public UnitTest1(WebApplicationFactory<Api1.Startup> webApplicationFactory, ITestOutputHelper output, TestOptions options = null)
        {
            this.webApplicationFactory = webApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, "Api1");

                builder.ConfigureServices(ConfigureLoggers);


            });
            rootWwebApplicationFactory = webApplicationFactory;
            this.output = output;
            this.options = options ?? new TestOptions();
        }

        public void Dispose()
        {
            rootWwebApplicationFactory.Dispose();
        }

        [Fact(DisplayName = "Call test service")]
        public async Task Fact1()
        {
            webApplicationFactory.CreateDefaultClient();

            ServiceCollection services = new ServiceCollection();

            ConfigureLoggers(services);

            services.Configure<MyHttpClientOptions>(o =>
            {
                o.BaseAddress = "https://localhost:5001";
            });

            services.AddHttpClient<IMyHttpClient, MyHttpClient>().ConfigureHttpMessageHandlerBuilder(builder =>
            {
                if (options.CallTestService)
                {
                    builder.PrimaryHandler = webApplicationFactory.Server.CreateHandler();
                }
            });

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                var logger = serviceProvider.GetRequiredService<ILogger<UnitTest1>>();

                var scope = new { requestId = "my-request-id", context = new { correlationId = "1234", someOtherData = "bla" } };

                var correlationContext = new Dictionary<string, object>() {
                    { nameof(scope.context.correlationId), scope.context.correlationId },
                    { nameof(scope.context.someOtherData), scope.context.someOtherData },
                };

                using (logger.BeginScope(scope))
                {
                    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/WeatherForecast")
                    {
                        Headers =
                        {
                            { "Correlation-Context", correlationContext.Select(FormattedPair) },
                            { "Request-Id", scope.requestId}
                        }
                    };
                    var myHttpClient1 = serviceProvider.GetService<IMyHttpClient>();

                    var response = await myHttpClient1.Client.SendAsync(httpRequestMessage);

                    Assert.True(response.IsSuccessStatusCode);
                }
            }
        }

        private void ConfigureLoggers(IServiceCollection services)
        {
            const LogLevel minLevel = LogLevel.Trace;

            services.AddLogging(logging =>
            {
                logging.ClearProviders();

                if (options.LogToTestOutput)
                {
                    logging.AddXunit(output, minLevel);
                }
                if (options.LogToConsole)
                {
                    logging.AddConsole();
                }

            });

            services.PostConfigureAll<ConsoleLoggerOptions>(options =>
            {
                options.IncludeScopes = true;
            });
            services.PostConfigureAll<LoggerFilterOptions>(options =>
            {
                options.MinLevel = minLevel;
                options.Rules.Clear();
            });

        }

        private static string FormattedPair(KeyValuePair<string, object> pair)
        {
            var (key, value) = pair;
            return $"{key}={value}";
        }

        [Fact(DisplayName = "Test Serilog configuration")]
        public void Fact2()
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

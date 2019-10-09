using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Api1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Parsing;
using Xunit;
using Xunit.Abstractions;
#if NETCOREAPP2_2
using HostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif
#if NETCOREAPP3_0
using HostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif
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
        public bool LogToTestOutput { get; set; } = false;
        public bool LogToSerilog { get; set; } = true;
        public string BaseAddress { get; set; } = "https://localhost:5001";
        public string EnvironmentName { get; set; } = "Testing";
    }

    public class UnitTest1 : IClassFixture<WebApplicationFactory<Api1.Startup>>, IDisposable
    {
        private readonly WebApplicationFactory<Api1.Startup> webApplicationFactory;
        private readonly IDisposable rootWebApplicationFactory;
        private readonly ITestOutputHelper output;
        private readonly TestOptions options;

        public UnitTest1(WebApplicationFactory<Api1.Startup> webApplicationFactory, ITestOutputHelper output, TestOptions options = null)
        {
            this.options = options ?? new TestOptions();

            this.webApplicationFactory = webApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting(WebHostDefaults.EnvironmentKey, this.options.EnvironmentName);
                //builder.UseSetting(WebHostDefaults.HostingStartupExcludeAssembliesKey, "Api1");

                builder.ConfigureServices(ConfigureLoggers);

            });
            rootWebApplicationFactory = webApplicationFactory;
            this.output = output;
        }

        public void Dispose()
        {
            rootWebApplicationFactory.Dispose();
        }

        [Fact(DisplayName = "Call test service")]
        public async Task Fact1()
        {
            ServiceCollection services = new ServiceCollection();

            ConfigureLoggers(services);

            services.Configure<MyHttpClientOptions>(o =>
            {
                o.BaseAddress = options.BaseAddress;
            });

            services.AddHttpClient<IMyHttpClient, MyHttpClient>().ConfigureHttpMessageHandlerBuilder(builder =>
            {
                if (options.CallTestService)
                {
                    webApplicationFactory.CreateDefaultClient();
                    builder.PrimaryHandler = webApplicationFactory.Server.CreateHandler();
                }
            });

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                ILogger<UnitTest1> logger = serviceProvider.GetRequiredService<ILogger<UnitTest1>>();

                var scope = new { requestId = "my-request-id", context = new { correlationId = "1234", someOtherData = "bla" } };

                Dictionary<string, object> correlationContext = new Dictionary<string, object>() {
                    { nameof(scope.context.correlationId), scope.context.correlationId },
                    { nameof(scope.context.someOtherData), scope.context.someOtherData },
                };

                using (logger.BeginScope(scope))
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/WeatherForecast")
                    {
                        Headers =
                        {
                            { "Correlation-Context", correlationContext.Select(FormattedPair) },
                            { "Request-Id", scope.requestId}
                        }
                    };
                    IMyHttpClient myHttpClient1 = serviceProvider.GetService<IMyHttpClient>();

                    HttpResponseMessage response = await myHttpClient1.Client.SendAsync(httpRequestMessage);

                    Assert.True(response.IsSuccessStatusCode);
                }
            }
        }
        class WebHostEnvironment : HostingEnvironment
        {
            public IFileProvider WebRootFileProvider { get; set; }
            public string WebRootPath { get; set; }
            public string ApplicationName { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; }
        }

        private void ConfigureLoggers(IServiceCollection services)
        {
            var builder = new ApplicationLoggerBuilder();

            IConfiguration configuration = new ConfigurationBuilder().Build();
            builder.ConfigureLoggingServices(configuration, services);

            var hostingEnvironment = new WebHostEnvironment() { EnvironmentName = this.options.EnvironmentName };

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
                if (options.LogToSerilog)
                {
                    var loggerConfiguration = new LoggerConfiguration();
                    builder.ConfigureLogging(loggerConfiguration, hostingEnvironment);

                    logging.AddSerilog(loggerConfiguration.CreateLogger());
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
            (string key, object value) = pair;
            return $"{key}={value}";
        }

        [Fact(DisplayName = "Test Serilog configuration")]
        public void Fact2()
        {
            LoggingLevelSwitch category1Switch = new LoggingLevelSwitch();

            bool CategoriesBelowCertainLevel(LogEvent e)
            {
                return Matching.FromSource("Category1").Invoke(e) && e.Level < category1Switch.MinimumLevel;
            }

            Logger logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .MinimumLevel.Verbose()
             .Filter.ByExcluding(CategoriesBelowCertainLevel)
              //.AuditTo.Sink()
              .WriteTo.Logger(configuration => configuration
                  .MinimumLevel.Warning()
                  .MinimumLevel.Override("Category1", LogEventLevel.Information)
                  .WriteTo.File("file1.log"))
              .WriteTo.Console()
              .CreateLogger();

            Serilog.ILogger category1Logger = logger.ForContext(Constants.SourceContextPropertyName, "Category1");
            Serilog.ILogger category2Logger = logger.ForContext(Constants.SourceContextPropertyName, "Category2");

            category1Logger.Information("visible");
            category2Logger.Information("invisible");

            category1Logger.Information("invisible");
            category2Logger.Information("visible");
        }

        [Fact(DisplayName = "Test Serilog configuration")]
        public void Fact4()
        {
            SelfLog.Enable(output.WriteLine);

            DummySink sink = new DummySink();

            Logger logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Sink(sink)
                .CreateLogger();

            Serilog.ILogger aLogger = logger.ForContext(Constants.SourceContextPropertyName, "A");

            aLogger.Information("ab");

            Assert.Collection(sink.Events, e => Assert.Equal("ab", e.MessageTemplate.Text));

            for (int i = 0; i < 100_000; i++)
            {
                aLogger.Information("ab");
            }
        }

        [Fact(DisplayName = "Should filter event with filter overrides")]
        public void Fact3()
        {
            DummySink sink = new DummySink();
            LoggerSourceContextLevelOverrides switches = new LoggerSourceContextLevelOverrides(LogEventLevel.Warning,
                KeyValuePair.Create("A", LogEventLevel.Debug),
                KeyValuePair.Create("2", LogEventLevel.Debug),
                KeyValuePair.Create("3", LogEventLevel.Debug),
                KeyValuePair.Create("4", LogEventLevel.Debug),
                KeyValuePair.Create("5", LogEventLevel.Debug),
                KeyValuePair.Create("6", LogEventLevel.Debug),
                KeyValuePair.Create("7", LogEventLevel.Debug),
                KeyValuePair.Create("8", LogEventLevel.Debug),
                KeyValuePair.Create("9", LogEventLevel.Debug),
                KeyValuePair.Create("10", LogEventLevel.Debug)
            );

            Logger logger = new LoggerConfiguration()
                .Filter().MinimumLevel()
                .WriteTo.Logger(c => c
                    .Filter().Overrides(switches)
                    .WriteTo.Sink(sink))
                .CreateLogger();

            Logger defaultLogger = new LoggerConfiguration()
                .Filter().MinimumLevel()
                .WriteTo.Logger(c => c
                    .Filter().Overrides(switches)
                    .WriteTo.Sink(sink))
                .CreateLogger();

            Serilog.ILogger abLogger = logger.ForContext(Constants.SourceContextPropertyName, "A.B");
            Serilog.ILogger bLogger = logger.ForContext(Constants.SourceContextPropertyName, "B");

            for (int i = 0; i < 100_000; i++)
            {
                bLogger.Information("b");
            }
            Assert.Empty(sink.Events);

            sink.Events.Clear();
            for (int i = 0; i < 100_000; i++)
            {
                abLogger.Debug("ab");
            }
            Assert.Equal(100000, sink.Events.Count);
            sink.Events.Clear();
        }

        [Fact(DisplayName = "Matching should work fast")]
        public void MyTestMethod()
        {
            Func<LogEvent, bool> testMatching = Matching.FromSource("test");

            LogEvent logEvent = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("message", new MessageTemplateToken[0]), new LogEventProperty[] {
                new LogEventProperty(Constants.SourceContextPropertyName, new ScalarValue("test"))
            }); ;

            bool result = testMatching.Invoke(logEvent);

            Assert.True(result);
            for (int i = 0; i < 100000; i++)
            {
                result = testMatching.Invoke(logEvent);
            }
        }

        private class DummySink : ILogEventSink
        {
            public List<LogEvent> Events { get; } = new List<LogEvent>();
            public void Emit(LogEvent logEvent)
            {
                Events.Add(logEvent);
            }
        }
    }

}

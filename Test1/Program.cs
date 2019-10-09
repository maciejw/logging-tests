using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Sdk;

namespace Test1
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            UnitTest1 test = new UnitTest1(new WebApplicationFactory<Api1.Startup>(), new TestOutputHelper(), new TestOptions
            {
                CallTestService = false,
                LogToConsole = false,
                LogToTestOutput = false,
                LogToSerilog = true,
                EnvironmentName = "Development"
            });
            await test.Fact1();
        }
    }
}

using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Test1
{
    public static class Program {

        public async static Task Main(string[] args)
        {
            var test = new UnitTest1(new WebApplicationFactory<Api1.Startup>(), new TestOutputHelper(), new TestOptions
            {
                CallTestService = false,
                LogToConsole = true,
                LogToTestOutput = false
            }); ;
            await test.Fact1();
        }
    }
}

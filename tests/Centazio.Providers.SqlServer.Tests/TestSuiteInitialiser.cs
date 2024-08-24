// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace

using Centazio.Providers.SqlServer;
using Centazio.Providers.SqlServer.Tests;

#pragma warning disable CA1050
[SetUpFixture] public class TestSuiteInitialiser {
#pragma warning restore CA1050
  
  [OneTimeSetUp] public async Task GlobalSetUp() {
    DapperInitialiser.Initialise();
    Centazio.Test.Lib.TestSuiteInitialiser.Initialise();
    await SqlConn.Instance.Init();
  }
  
  [OneTimeTearDown] public async Task GlobalTearDown() {
    await SqlConn.Instance.Dispose();
  }

}
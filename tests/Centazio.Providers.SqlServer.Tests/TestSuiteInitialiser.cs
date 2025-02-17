// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace

using Centazio.Core;
using Centazio.Test.Lib;

#pragma warning disable CA1050
namespace Centazio.Providers.SqlServer.Tests;

[SetUpFixture] public class TestSuiteInitialiser {
#pragma warning restore CA1050
  
  [OneTimeSetUp] public void GlobalSetUp() {
    GlobalTestSuiteInitialiser.Init();
  }
  
  [OneTimeTearDown] public async Task GlobalTearDown() {
    await (await SqlConn.GetInstance(false, CentazioConstants.DEFAULT_ENVIRONMENT)).Dispose();
  }

}
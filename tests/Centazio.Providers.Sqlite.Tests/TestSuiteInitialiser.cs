// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace

using Centazio.Test.Lib;

#pragma warning disable CA1050
namespace Centazio.Providers.Sqlite.Tests;

[SetUpFixture] public class TestSuiteInitialiser {
#pragma warning restore CA1050
  
  [OneTimeSetUp] public void GlobalSetUp() {
    DapperInitialiser.Initialise();
    GlobalTestSuiteInitialiser.Init();
  }
}
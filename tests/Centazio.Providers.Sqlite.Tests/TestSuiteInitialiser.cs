// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace

using Centazio.Providers.Sqlite;
using Centazio.Test.Lib;

#pragma warning disable CA1050
[SetUpFixture] public class TestSuiteInitialiser {
#pragma warning restore CA1050
  
  [OneTimeSetUp] public void GlobalSetUp() {
    DapperInitialiser.Initialise();
    GlobalTestSuiteInitialiser.Init();
  }
}
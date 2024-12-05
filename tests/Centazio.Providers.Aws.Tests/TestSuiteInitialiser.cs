// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace

using Centazio.Test.Lib;

#pragma warning disable CA1050
[SetUpFixture] public class TestSuiteInitialiser {
#pragma warning restore CA1050
  [OneTimeSetUp] public void GlobalSetUp() {
    Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", "x");
    Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "x");
    Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "x");
    
    GlobalTestSuiteInitialiser.Init();
  }

}
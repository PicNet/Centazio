// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace
#pragma warning disable CA1050
[SetUpFixture] public class Global {
#pragma warning restore CA1050
  [OneTimeSetUp] public void GlobalSetUp() => Centazio.Test.Lib.TestSuiteInitialiser.Initialise();
}
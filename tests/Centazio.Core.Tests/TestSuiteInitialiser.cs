// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace
[SetUpFixture] public class Global {
  [OneTimeSetUp] public void GlobalSetUp() => Centazio.Test.Lib.TestSuiteInitialiser.Initialise();
}
global using F = Centazio.Test.Lib.TestingFactories;

using Centazio.Test.Lib;

[SetUpFixture] public class Global {
  [OneTimeSetUp] public void GlobalSetUp() => GlobalTestSuiteInitialiser.Init();
}
global using F = Centazio.Test.Lib.TestingFactories;
global using SC = Centazio.Sample.SampleConstants;

using Centazio.Test.Lib;

[SetUpFixture] public class Global {
  [OneTimeSetUp] public void GlobalSetUp() => GlobalTestSuiteInitialiser.Init();
}
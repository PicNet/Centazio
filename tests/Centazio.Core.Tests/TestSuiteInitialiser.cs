// this class purposefully omits the namespace to ensure that the initialisation below
//  occurs for all tests, reagardless of namespace

using Centazio.Test.Lib;

namespace Centazio.Core.Tests;

[SetUpFixture] public class Global {
  [OneTimeSetUp] public void GlobalSetUp() => GlobalTestSuiteInitialiser.Init();
}
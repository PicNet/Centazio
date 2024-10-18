using Centazio.Test.Lib;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Core.Tests.CoreToSystemMapping;

public class InMemoryCoreToSystemMapStoreTests : AbstractCoreToSystemMapStoreTests {
  protected override Task<ITestingCoreToSystemMapStore> GetStore() => Task.FromResult<ITestingCoreToSystemMapStore>(new TestingInMemoryCoreToSystemMapStore());
}
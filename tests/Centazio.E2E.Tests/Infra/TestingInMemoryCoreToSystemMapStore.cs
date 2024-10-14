using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Ctl.Entities;

namespace Centazio.E2E.Tests.Infra;

public class TestingInMemoryCoreToSystemMapStore : InMemoryCoreToSystemMapStore {
  public Dictionary<Map.Key, string> Db => memdb;
}
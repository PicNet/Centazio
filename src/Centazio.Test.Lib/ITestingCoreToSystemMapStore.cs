using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Test.Lib;

public interface ITestingCoreToSystemMapStore : ICoreToSystemMapStore{
  Task<List<Map.CoreToSystemMap>> GetAll();
}
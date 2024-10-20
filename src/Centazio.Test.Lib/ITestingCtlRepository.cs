using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Test.Lib;

public interface ITestingCtlRepository : ICtlRepository {
  Task<List<Map.CoreToSysMap>> GetAllMaps();
}
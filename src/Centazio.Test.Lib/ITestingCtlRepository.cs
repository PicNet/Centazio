namespace Centazio.Test.Lib;

public interface ITestingCtlRepository : ICtlRepository {
  Task<List<Map.CoreToSysMap>> GetAllMaps();
}
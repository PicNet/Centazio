using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib;

public interface ITestingCoreStorage : ICoreStorage {
  Task<List<CoreEntity>> GetAllCoreEntities();
}
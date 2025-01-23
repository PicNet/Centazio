using Centazio.Core.CoreRepo;
using Centazio.Core.Types;

namespace Centazio.Test.Lib.E2E;

public interface ISimulationCoreStorageRepository : ICoreStorage {
  Task<List<CoreMembershipType>> GetMembershipTypes();
  Task<List<CoreCustomer>> GetCustomers();
  Task<List<CoreInvoice>> GetInvoices();
  
  Task<CoreMembershipType> GetMembershipType(CoreEntityId coreid);

}
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Test.Lib.E2E;

public interface ISimulationCoreStorageRepository : ICoreStorage {
  Task<List<CoreMembershipType>> GetMembershipTypes();
  Task<List<CoreCustomer>> GetCustomers();
  Task<List<CoreInvoice>> GetInvoices();
}

public abstract class AbstractCoreStorageRepository(Func<ICoreEntity, CoreEntityChecksum> checksum) : ISimulationCoreStorageRepository {
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetEntitiesToWrite<CoreMembershipType, CoreMembershipType.Dto>(exclude, after)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetEntitiesToWrite<CoreCustomer, CoreCustomer.Dto>(exclude, after)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetEntitiesToWrite<CoreInvoice, CoreInvoice.Dto>(exclude, after)).ToList();
    throw new NotSupportedException(coretype);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => (await GetExistingEntities(coretype, coreids)).ToDictionary(e => e.CoreEntity.CoreId, e => checksum(e.CoreEntity));

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetExistingEntities<CoreMembershipType, CoreMembershipType.Dto>(coreids)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetExistingEntities<CoreCustomer, CoreCustomer.Dto>(coreids)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetExistingEntities<CoreInvoice, CoreInvoice.Dto>(coreids)).ToList();
    throw new NotSupportedException(coretype);
  }

  public abstract Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities);
  
  // Simulation Specific Methods
  
  public async Task<CoreMembershipType> GetMembershipType(CoreEntityId coreid) => await GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);
  public async Task<List<CoreMembershipType>> GetMembershipTypes() => await GetAll<CoreMembershipType, CoreMembershipType.Dto>();
  public async Task<CoreCustomer> GetCustomer(CoreEntityId coreid) => await GetSingle<CoreCustomer, CoreCustomer.Dto>(coreid);
  public async Task<List<CoreCustomer>> GetCustomers() => await GetAll<CoreCustomer, CoreCustomer.Dto>();
  public async Task<CoreInvoice> GetInvoice(CoreEntityId coreid) => await GetSingle<CoreInvoice, CoreInvoice.Dto>(coreid);
  public async Task<List<CoreInvoice>> GetInvoices() => await GetAll<CoreInvoice, CoreInvoice.Dto>();
  
  protected abstract Task<E> GetSingle<E, D>(CoreEntityId coreid) where E : CoreEntityBase where D : class, ICoreEntityDto<E>;
  
  protected abstract Task<List<CoreEntityAndMeta>> GetExistingEntities<E, D>(List<CoreEntityId> coreids) where E : CoreEntityBase where D : CoreEntityBase.Dto<E>;
  protected abstract Task<List<CoreEntityAndMeta>> GetEntitiesToWrite<E, D>(SystemName exclude, DateTime after) where E : CoreEntityBase where D : CoreEntityBase.Dto<E>;
  
  private async Task<List<E>> GetAll<E, D>() where E : CoreEntityBase where D : CoreEntityBase.Dto<E> => 
      (await GetEntitiesToWrite<E, D>(new("ignore"), DateTime.MinValue)).Select(c => (E) c.CoreEntity).ToList();
  
  public abstract ValueTask DisposeAsync();
}
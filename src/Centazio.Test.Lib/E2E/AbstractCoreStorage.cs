using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.E2E;

public interface ISimulationCoreStorage : ICoreStorage {
  Task<CoreMembershipType?> GetMembershipType(CoreEntityId? coreid);
  Task<List<CoreMembershipType>> GetMembershipTypes();
  Task<CoreCustomer?> GetCustomer(CoreEntityId? coreid);
  Task<List<CoreCustomer>> GetCustomers();
  Task<CoreInvoice?> GetInvoice(CoreEntityId? coreid);
  Task<List<CoreInvoice>> GetInvoices();
}

public abstract class AbstractCoreStorage(Func<ICoreEntity, CoreEntityChecksum> checksum) : ISimulationCoreStorage {
  
  public async Task<List<ICoreEntity>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) => 
      (await GetList(coretype)).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList();

  public async Task<List<ICoreEntity>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      (await GetList(coretype)).Where(e => coreids.Contains(e.CoreId)).ToList();

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      (await GetList(coretype)).Where(e => coreids.Contains(e.CoreId)).ToDictionary(e => e.CoreId, checksum);
  
  public abstract Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities);
  
  public abstract ValueTask DisposeAsync();
  
  // Simulation Specific Methods
  
  public async Task<CoreMembershipType?> GetMembershipType(CoreEntityId? coreid) => await GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);
  public async Task<List<CoreMembershipType>> GetMembershipTypes() => await GetList<CoreMembershipType, CoreMembershipType.Dto>();
  public async Task<CoreCustomer?> GetCustomer(CoreEntityId? coreid) => await GetSingle<CoreCustomer, CoreCustomer.Dto>(coreid);
  public async Task<List<CoreCustomer>> GetCustomers() => await GetList<CoreCustomer, CoreCustomer.Dto>();
  public async Task<CoreInvoice?> GetInvoice(CoreEntityId? coreid) => await GetSingle<CoreInvoice, CoreInvoice.Dto>(coreid);
  public async Task<List<CoreInvoice>> GetInvoices() => await GetList<CoreInvoice, CoreInvoice.Dto>();

  private async Task<List<ICoreEntity>> GetList(CoreEntityTypeName coretype) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetList<CoreMembershipType, CoreMembershipType.Dto>()).Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetList<CoreCustomer, CoreCustomer.Dto>()).Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetList<CoreInvoice, CoreInvoice.Dto>()).Cast<ICoreEntity>().ToList();
    throw new NotSupportedException(coretype);
  }
  
  protected abstract Task<E> GetSingle<E, D>(CoreEntityId? coreid) where E : CoreEntityBase where D : class, ICoreEntityDto<E>;
  
  // todo: GetList is a `SELECT *` so needs to be removed
  protected abstract Task<List<E>> GetList<E, D>() where E : CoreEntityBase where D : CoreEntityBase.Dto<E>;
}
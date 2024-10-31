using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.E2E;

public interface ISimulationCoreStorageRepository : ICoreStorage {
  Task<List<CoreMembershipType>> GetMembershipTypes(Expression<Func<CoreMembershipType.Dto, bool>> predicate);
  Task<List<CoreCustomer>> GetCustomers(Expression<Func<CoreCustomer.Dto, bool>> predicate);
  Task<List<CoreInvoice>> GetInvoices(Expression<Func<CoreInvoice.Dto, bool>> predicate);
}

public abstract class AbstractCoreStorageRepository(Func<ICoreEntity, CoreEntityChecksum> checksum) : ISimulationCoreStorageRepository {
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetList<CoreMembershipType, CoreMembershipType.Dto>(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetList<CoreCustomer, CoreCustomer.Dto>(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetList<CoreInvoice, CoreInvoice.Dto>(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after)).ToList();
    throw new NotSupportedException(coretype);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => (await GetExistingEntities(coretype, coreids)).ToDictionary(e => e.CoreEntity.CoreId, e => checksum(e.CoreEntity));

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetList<CoreMembershipType, CoreMembershipType.Dto>(e => strids.Contains(e.CoreId))).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetList<CoreCustomer, CoreCustomer.Dto>(e => strids.Contains(e.CoreId))).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetList<CoreInvoice, CoreInvoice.Dto>(e => strids.Contains(e.CoreId))).ToList();
    throw new NotSupportedException(coretype);
  }

  public abstract Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<(CoreEntityAndMeta UpdatedCoreEntityAndMeta, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities);
  
  // Simulation Specific Methods
  
  public async Task<CoreMembershipType> GetMembershipType(CoreEntityId coreid) => await GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);
  public async Task<List<CoreMembershipType>> GetMembershipTypes(Expression<Func<CoreMembershipType.Dto, bool>> predicate) => await GetEntityList<CoreMembershipType, CoreMembershipType.Dto>(predicate);
  public async Task<CoreCustomer> GetCustomer(CoreEntityId coreid) => await GetSingle<CoreCustomer, CoreCustomer.Dto>(coreid);
  public async Task<List<CoreCustomer>> GetCustomers(Expression<Func<CoreCustomer.Dto, bool>> predicate) => await GetEntityList<CoreCustomer, CoreCustomer.Dto>(predicate);
  public async Task<CoreInvoice> GetInvoice(CoreEntityId coreid) => await GetSingle<CoreInvoice, CoreInvoice.Dto>(coreid);
  public async Task<List<CoreInvoice>> GetInvoices(Expression<Func<CoreInvoice.Dto, bool>> predicate) => await GetEntityList<CoreInvoice, CoreInvoice.Dto>(predicate);
  
  protected abstract Task<E> GetSingle<E, D>(CoreEntityId coreid) where E : CoreEntityBase where D : class, ICoreEntityDto<E>;
  
  protected async Task<List<E>> GetEntityList<E, D>(Expression<Func<D, bool>> predicate) where E : CoreEntityBase where D : CoreEntityBase.Dto<E> => (await GetList<E, D>(predicate)).Select(t => (E) t.CoreEntity).ToList();
  protected abstract Task<List<CoreEntityAndMeta>> GetList<E, D>(Expression<Func<D, bool>> predicate) where E : CoreEntityBase where D : CoreEntityBase.Dto<E>;
  
  public abstract ValueTask DisposeAsync();
}
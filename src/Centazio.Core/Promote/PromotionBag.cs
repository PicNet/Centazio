using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Serilog;

namespace Centazio.Core.Promote;

public class PromotionBag(StagedEntity staged) {
  public StagedEntity StagedEntity { get; init; } = staged;
  public ISystemEntity SystemEntity { get; set; } = null!;
  public Map.CoreToSystemMap? Map { get; set; }
  public ICoreEntity? PreExistingCoreEntity { get; set; }
  public CoreEntityChecksum? PreExistingCoreEntityChecksum { get; set; }
  public CoreEntityChecksum? UpdatedCoreEntityChecksum { get; set; }
  
  public ICoreEntity? UpdatedCoreEntity { get; internal set; }
  public ValidString? IgnoreReason { get; private set; }
  
  public void MarkIgnore(ValidString reason) {
    Log.Debug($"PromotionBag.MarkIgnore[{UpdatedCoreEntity?.DisplayName}] - reason[{reason}]");
    
    UpdatedCoreEntity = null;
    IgnoreReason = reason;
  } 
  
  public void MarkPromote(SystemName system, ICoreEntity coreent) {
    UpdatedCoreEntity = CheckAndSetInternalState();
    
    ICoreEntity CheckAndSetInternalState() {
      if (PreExistingCoreEntity is not null && PreExistingCoreEntity.SystemId != coreent.SystemId) throw new Exception($"PromoteEvaluator.BuildCoreEntities should never change the core entities SystemId");
      
      coreent.DateUpdated = UtcDate.UtcNow;
      coreent.LastUpdateSystem = system;
      if (!IsCreating) return coreent;

      coreent.DateCreated = UtcDate.UtcNow;
      coreent.System = system;
      return coreent;
    }
  }
  
  public void CorrectBounceBackIds(ICoreEntity coreent) {
    UpdatedCoreEntity!.CoreId = coreent.CoreId;
    UpdatedCoreEntity!.SystemId = coreent.SystemId;
  }

  // todo: since this is now abstracted nicely, do we still need Map.PendingCreate
  public Map.Created MarkCreated(IChecksumAlgorithm checksum) => Ctl.Entities.Map.Create((UpdatedCoreEntity ?? throw new Exception()).System, UpdatedCoreEntity).SuccessCreate(UpdatedCoreEntity!.SystemId, checksum.Checksum(SystemEntity));
  public Map.Updated MarkUpdated(IChecksumAlgorithm checksum) => (Map ?? throw new Exception()).Update().SuccessUpdate(checksum.Checksum(SystemEntity));
  
  public bool IsCreating => PreExistingCoreEntity is null;
  public bool IsIgnore => IgnoreReason is not null;

}
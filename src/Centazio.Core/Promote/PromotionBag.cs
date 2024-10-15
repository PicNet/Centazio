using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Serilog;

namespace Centazio.Core.Promote;

public class PromotionBag(StagedEntity staged) {
  public StagedEntity StagedEntity { get; init; } = staged;
  public ISystemEntity Sys { get; set; } = null!;
  public ICoreEntity? ExistingCoreEntity { get; set; }
  [IgnoreNamingConventions] public CoreEntityChecksum? UpdatedCoreEntityChecksum { get; set; }
  
  public ICoreEntity? UpdatedCoreEntity { get; internal set; }
  public ValidString? IgnoreReason { get; private set; }
  
  public void MarkIgnore(ValidString reason) {
    Log.Debug($"PromotionBag.MarkIgnore[{UpdatedCoreEntity?.DisplayName}] - reason[{reason}]");
    
    UpdatedCoreEntity = null;
    IgnoreReason = reason;
  } 
  
  public void MarkPromote(SystemName system, ICoreEntity coreent) {
    UpdatedCoreEntity = CheckAndSetInternalState(coreent);
    
    ICoreEntity CheckAndSetInternalState(ICoreEntity e) {
      if (ExistingCoreEntity is not null && ExistingCoreEntity.SystemId != e.SystemId) throw new Exception($"PromoteEvaluator.BuildCoreEntities should never change the core entities SystemId");
      
      e.DateUpdated = UtcDate.UtcNow;
      e.LastUpdateSystem = system;
      if (!IsCreating) return e;

      e.DateCreated = UtcDate.UtcNow;
      e.System = system;
      return e;
    }
  }
  
  public void CorrectBounceBackIds(ICoreEntity orig) {
    UpdatedCoreEntity!.CoreId = orig.CoreId;
    UpdatedCoreEntity!.SystemId = orig.SystemId;
  }
      
  public bool IsCreating => ExistingCoreEntity is null;
  public bool IsIgnore => IgnoreReason is not null;
}
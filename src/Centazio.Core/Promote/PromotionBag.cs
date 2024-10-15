using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Serilog;

namespace Centazio.Core.Promote;

public class PromotionBag(StagedEntity staged) {
  public StagedEntity StagedEntity { get; init; } = staged;
  public ISystemEntity SystemEntity { get; set; } = null!;
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
    UpdatedCoreEntity = CheckAndSetInternalState();
    
    ICoreEntity CheckAndSetInternalState() {
      if (ExistingCoreEntity is not null && ExistingCoreEntity.SystemId != coreent.SystemId) throw new Exception($"PromoteEvaluator.BuildCoreEntities should never change the core entities SystemId");
      
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
      
  public bool IsCreating => ExistingCoreEntity is null;
  public bool IsIgnore => IgnoreReason is not null;
}
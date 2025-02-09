using System.Diagnostics.CodeAnalysis;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Serilog;

namespace Centazio.Core.Promote;

public class PromotionBag(StagedEntity staged) {
  public StagedEntity StagedEntity { get; init; } = staged;
  public ISystemEntity SystemEntity { get; set; } = null!;
  public Map.CoreToSysMap? Map { get; set; }
  public CoreEntityAndMeta? PreExistingCoreEntityAndMeta { get; set; }
  public CoreEntityChecksum? PreExistingCoreEntityChecksum { get; set; }
  public CoreEntityAndMeta? UpdatedCoreEntityAndMeta { get; internal set; }
  public ValidString? IgnoreReason { get; private set; }
  
  public CoreEntityAndMeta CoreEntityAndMeta => UpdatedCoreEntityAndMeta ?? throw new Exception();
  
  public void MarkIgnore(ValidString reason) {
    Log.Debug($"PromotionBag.MarkIgnore[{UpdatedCoreEntityAndMeta?.CoreEntity.GetShortDisplayName()}] - reason[{reason}]");
    
    UpdatedCoreEntityAndMeta = null;
    IgnoreReason = reason;
  } 
  
  public void MarkPromote(SystemName system, CoreEntityAndMeta coreent, IChecksumAlgorithm checksum) {
    UpdatedCoreEntityAndMeta = CheckAndSetInternalState();
    
    CoreEntityAndMeta CheckAndSetInternalState() => coreent with {
      Meta = coreent.Meta with {
        DateUpdated = UtcDate.UtcNow,
        LastUpdateSystem = system,
        DateCreated = IsCreating ? UtcDate.UtcNow : coreent.Meta.DateCreated,
        OriginalSystem = IsCreating ? system : coreent.Meta.OriginalSystem,
        CoreEntityChecksum = checksum.Checksum(coreent.CoreEntity)
      }
    };
  }
  
  public void CorrectBounceBackIds(CoreEntityAndMeta coreent) {
    if (UpdatedCoreEntityAndMeta is null) throw new Exception();
    UpdatedCoreEntityAndMeta = UpdatedCoreEntityAndMeta with {
      Meta = UpdatedCoreEntityAndMeta.Meta with {
        CoreId = coreent.Meta.CoreId,
        OriginalSystemId = coreent.Meta.OriginalSystemId,
      }
    }; 
  }

  public Map.Created MarkCreated(IChecksumAlgorithm checksum) => Ctl.Entities.Map.Create(CoreEntityAndMeta.Meta.OriginalSystem, CoreEntityAndMeta.CoreEntity)
      .SuccessCreate(CoreEntityAndMeta.Meta.OriginalSystemId, checksum.Checksum(SystemEntity));
  public Map.Updated MarkUpdated(IChecksumAlgorithm checksum) => (Map ?? throw new Exception()).Update().SuccessUpdate(checksum.Checksum(SystemEntity));
  
  public bool IsCreating => PreExistingCoreEntityAndMeta is null;
  
  [MemberNotNullWhen(true, nameof(IgnoreReason))] public bool IsIgnore => IgnoreReason is not null;

}
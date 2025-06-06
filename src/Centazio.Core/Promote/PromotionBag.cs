﻿using System.Diagnostics.CodeAnalysis;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Stage;

namespace Centazio.Core.Promote;

public class PromotionBag(StagedEntity staged, Type setype) {
  public StagedEntity StagedEntity { get; } = staged;
  public ISystemEntity SystemEntity { get; internal set; } = (ISystemEntity) Json.Deserialize(staged.Data, setype);
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
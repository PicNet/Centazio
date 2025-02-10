using Centazio.Core.Misc;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Misc;

public class DtoHelpersTests {

  [Test] public void Test_ToDto() {
    var se = new StagedEntity(Guid.NewGuid(), C.System1Name, C.SystemEntityName, UtcDate.UtcNow, new(Guid.NewGuid().ToString()), new(Guid.NewGuid().ToString()));
    Assert.That(DtoHelpers.ToDto(se), Is.EqualTo(OriginalToDto(se)));
    Assert.That((DtoHelpers.ToDto(se) as StagedEntity.Dto ?? throw new Exception()).ToBase(), Is.EqualTo(se));
    
    StagedEntity.Dto OriginalToDto(StagedEntity e) => new() {
      Id = e.Id,
      System = e.System.Value,
      SystemEntityTypeName = e.SystemEntityTypeName.Value,
      DateStaged = e.DateStaged,
      Data = e.Data.Value,
      StagedEntityChecksum = e.StagedEntityChecksum.Value,
      DatePromoted = e.DatePromoted,
      IgnoreReason = e.IgnoreReason
    };
  }
  
  [Test] public void Test_Generic_ToDto() {
    var se = new StagedEntity(Guid.NewGuid(), C.System1Name, C.SystemEntityName, UtcDate.UtcNow, new(Guid.NewGuid().ToString()), new(Guid.NewGuid().ToString()));
    Assert.That(DtoHelpers.ToDto<StagedEntity, StagedEntity.Dto>(se), Is.EqualTo(OriginalToDto(se)));
    Assert.That(DtoHelpers.ToDto<StagedEntity, StagedEntity.Dto>(se).ToBase(), Is.EqualTo(se));
    
    StagedEntity.Dto OriginalToDto(StagedEntity e) => new() {
      Id = e.Id,
      System = e.System.Value,
      SystemEntityTypeName = e.SystemEntityTypeName.Value,
      DateStaged = e.DateStaged,
      Data = e.Data.Value,
      StagedEntityChecksum = e.StagedEntityChecksum.Value,
      DatePromoted = e.DatePromoted,
      IgnoreReason = e.IgnoreReason
    };   
  }

  
}
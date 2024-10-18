using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests;

public class DtoHelpersTests {

  [Test] public void Test_ToDto() {
    
    var se = new StagedEntity(Guid.NewGuid(), Constants.System1Name, Constants.SystemEntityName, UtcDate.UtcNow, Guid.NewGuid().ToString(), new(Guid.NewGuid().ToString()));
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

  
}
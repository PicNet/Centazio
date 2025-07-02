using System.ComponentModel.DataAnnotations;

namespace Centazio.Test.Lib;

[IgnoreNamingConventions] 
public static class Constants {
  public static readonly SystemName System1Name = new("CRM");
  public static readonly SystemName System2Name = new("FIN");
  public static readonly SystemEntityId Sys1Id1 = new("S1.1");
  public static readonly SystemEntityId Sys1Id2 = new("S1.2");
  public static readonly SystemEntityTypeName SystemEntityName = new("SE1");
  public static readonly SystemEntityTypeName SystemEntityName2 = new("SE2");
  public static readonly CoreEntityTypeName CoreEntityName = CoreEntityTypeName.From<CoreEntity>();
  public static readonly CoreEntityTypeName CoreEntityName2 = CoreEntityTypeName.From<CoreEntity2>();
  public static readonly CoreEntityId CoreE1Id1 = new("C1.1");
  public static readonly CoreEntityId CoreE1Id2 = new("C1.2");
}

public record System1Entity(Guid Sys1EntityId, string FirstName, string LastName, DateOnly DateOfBirth, DateTime LastUpdatedDate) : ISystemEntity {

  public SystemEntityId SystemId => new(Sys1EntityId.ToString());
  public string DisplayName => $"{FirstName} {LastName}({Sys1EntityId})";
  public object GetChecksumSubset() => new { SystemId, FirstName, LastName, DateOfBirth };
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { Sys1EntityId = Guid.Parse(newid.Value) };
  public CoreEntity ToCoreEntity() => new(new(SystemId.Value), FirstName, LastName, DateOfBirth);
}

public record CoreEntity(CoreEntityId CoreId, string FirstName, string LastName, DateOnly DateOfBirth) : CoreEntityBase(CoreId) {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  [MaxLength(64)] public string FirstName { get; init; } = FirstName;
  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  [MaxLength(64)] public string LastName { get; init; } = LastName;
  
  public override string DisplayName => $"{FirstName} {LastName}";
  
  public override object GetChecksumSubset() => new { CoreId, FirstName, LastName, DateOfBirth };
}

public record CoreEntity2(CoreEntityId CoreId, DateTime DateUpdated) : ICoreEntity {
  public CoreEntityId CoreId { get; set; } = CoreId;
  
  public string DisplayName { get; } = CoreId;
  
  public object GetChecksumSubset() => new { CoreId, Id = CoreId };
}
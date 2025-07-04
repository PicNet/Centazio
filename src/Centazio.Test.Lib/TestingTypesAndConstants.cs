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

public record System1Entity(Guid Sys1EntityId, CorrelationId CorrelationId, string FirstName, string LastName, DateOnly DateOfBirth, DateTime LastUpdatedDate) : ISystemEntity {

  public SystemEntityId SystemId => new(Sys1EntityId.ToString());
  public string DisplayName => $"{FirstName} {LastName}({Sys1EntityId})";
  public object GetChecksumSubset() => new { SystemId, FirstName, LastName, DateOfBirth };
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { Sys1EntityId = Guid.Parse(newid.Value) };
  public CoreEntity ToCoreEntity() => new(new(SystemId.Value), CorrelationId, FirstName, LastName, DateOfBirth);
}

public record CoreEntity(CoreEntityId CoreId, CorrelationId CorrelationId, string FirstName, string LastName, DateOnly DateOfBirth) : CoreEntityBase(CoreId, CorrelationId) {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  [MaxLength(64)] public string FirstName { get; init; } = FirstName;
  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  [MaxLength(64)] public string LastName { get; init; } = LastName;
  
  public override string DisplayName => $"{FirstName} {LastName}";
  
  public override object GetChecksumSubset() => new { CoreId, FirstName, LastName, DateOfBirth };

  public record Dto : ICoreEntityDto<CoreEntity> {
    public required string CoreId { get; init; }
    public required string CorrelationId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    
    public CoreEntity ToBase() => new(
        new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
        new(CorrelationId ?? throw new ArgumentNullException(nameof(Core.CorrelationId))),
        FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
        LastName ?? throw new ArgumentNullException(nameof(LastName)),
        DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)));
  }
}

public record CoreEntity2(CoreEntityId CoreId, CorrelationId CorrelationId, DateTime DateUpdated) : ICoreEntity {
  public CoreEntityId CoreId { get; set; } = CoreId;
  public CorrelationId CorrelationId { get; set; } = CorrelationId;
  
  public string DisplayName { get; } = CoreId;
  
  public object GetChecksumSubset() => new { CoreId, Id = CoreId };
}
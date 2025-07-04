using System.ComponentModel.DataAnnotations;
using Centazio.Core.Checksum;

namespace Centazio.Core.Stage;

public record StagedEntity {
  
  public static StagedEntity Create(SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, StagedEntityChecksum checksum) => Create(Guid.CreateVersion7(), system, systype, staged, data, checksum);
  public static StagedEntity Create(Guid id, SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, StagedEntityChecksum checksum) => new(id, system, systype, staged, data, checksum);
  
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  public StagedEntity Ignore(ValidString reason) => this with { IgnoreReason = reason };
  
  internal StagedEntity(Guid id, SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, StagedEntityChecksum checksum, DateTime? promoted = null, string? ignore = null) {
    Id = id;
    System = system;
    SystemEntityTypeName = systype;
    DateStaged = staged;
    Data = data;
    StagedEntityChecksum = checksum;
    
    DatePromoted = promoted;
    IgnoreReason = ignore;
  }
  
  public Guid Id { get; }
  public SystemName System { get; }
  public SystemEntityTypeName SystemEntityTypeName { get; }
  public DateTime DateStaged { get; }
  [MaxLength(Int32.MaxValue)] public ValidString Data { get; internal init; }
  public StagedEntityChecksum StagedEntityChecksum { get; }
  [MaxLength(1024)] public string? IgnoreReason { get; internal init; }
  
  public DateTime? DatePromoted { get; internal init; }
  
  public T Deserialise<T>() => Json.Deserialize<T>(Data) ?? throw new Exception();
  
}

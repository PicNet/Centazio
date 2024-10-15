using Centazio.Core.Checksum;

namespace Centazio.Core.Ctl.Entities;

public sealed record StagedEntity {
  
  public static StagedEntity Create(SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, StagedEntityChecksum checksum) => new(Guid.CreateVersion7(), system, systype, staged, data, checksum);
  
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  public StagedEntity Ignore(ValidString reason) => this with { IgnoreReason = reason };
  
  internal StagedEntity(Guid id, SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, StagedEntityChecksum checksum) {
    Id = id;
    System = system;
    SystemEntityTypeName = systype;
    DateStaged = staged;
    Data = data;
    StagedEntityChecksum = checksum;
  }
  
  public Guid Id { get; }
  public SystemName System { get; }
  public SystemEntityTypeName SystemEntityTypeName { get; }
  public DateTime DateStaged { get; }
  public ValidString Data { get; internal init; }
  public StagedEntityChecksum StagedEntityChecksum { get; }
  public string? IgnoreReason { get; internal init; }
  
  public DateTime? DatePromoted { get; internal init; }
  
  public T Deserialise<T>() => Json.Deserialize<T>(Data) ?? throw new Exception();

  public Dto ToDto() => new() {
    Id = Id,
    System = System.Value,
    SystemEntityTypeName = SystemEntityTypeName.Value,
    DateStaged = DateStaged,
    Data = Data.Value,
    StagedEntityChecksum = StagedEntityChecksum.Value,
    DatePromoted = DatePromoted,
    IgnoreReason = IgnoreReason
  };
  
  public record Dto : IDto<StagedEntity> {
    public Guid? Id { get; init; }
    public string? System { get; init; }
    public string? SystemEntityTypeName { get; init; }
    public DateTime? DateStaged { get; init; }
    public string? Data { get; init; }
    public string? StagedEntityChecksum { get; init; }
    public DateTime? DatePromoted { get; init; }
    public string? IgnoreReason { get; init; }
    
    public Dto() {}
    
    internal Dto(Guid? id, string syste, string obj, DateTime? staged, string? data, string? checksum, DateTime? promoted = null, string? ignoreres = null) {
      Id = id;
      System = syste;
      SystemEntityTypeName = obj;
      DateStaged = staged;
      Data = data;
      StagedEntityChecksum = checksum;
      DatePromoted = promoted;
      IgnoreReason = ignoreres;
    }
    
    public StagedEntity ToBase() => new(
        Id ?? throw new ArgumentNullException(nameof(Id)),
        System ?? throw new ArgumentNullException(nameof(System)),
        new(SystemEntityTypeName ?? throw new ArgumentNullException(nameof(SystemEntityTypeName))),
        DateStaged ?? throw new ArgumentNullException(nameof(DateStaged)),
        Data ?? throw new ArgumentNullException(nameof(Data)),
        new(StagedEntityChecksum ?? throw new ArgumentNullException(nameof(StagedEntityChecksum)))) {
      IgnoreReason = String.IsNullOrWhiteSpace(IgnoreReason) ? null : IgnoreReason.Trim(),
      DatePromoted = DatePromoted
    };
  }
}

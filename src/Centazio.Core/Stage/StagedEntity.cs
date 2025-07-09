using System.ComponentModel.DataAnnotations;
using Centazio.Core.Checksum;

namespace Centazio.Core.Stage;

public record StagedEntity {
  
  public static StagedEntity Create(SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, CorrelationId corrid, StagedEntityChecksum checksum) => new(Guid.CreateVersion7(), system, systype, staged, data, corrid, checksum);
  
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  public StagedEntity Ignore(ValidString reason) => this with { IgnoreReason = reason };
  
  internal StagedEntity(Guid id, SystemName system, SystemEntityTypeName systype, DateTime staged, ValidString data, CorrelationId corrid, StagedEntityChecksum checksum) {
    Id = id;
    System = system;
    SystemEntityTypeName = systype;
    DateStaged = staged;
    Data = data;
    CorrelationId = corrid;
    StagedEntityChecksum = checksum;
  }
  
  public Guid Id { get; }
  public CorrelationId CorrelationId { get; }
  public SystemName System { get; }
  public SystemEntityTypeName SystemEntityTypeName { get; }
  public DateTime DateStaged { get; }
  [MaxLength(Int32.MaxValue)] public ValidString Data { get; internal init; }
  public StagedEntityChecksum StagedEntityChecksum { get; }
  [MaxLength(1024)] public string? IgnoreReason { get; internal init; }
  
  public DateTime? DatePromoted { get; internal init; }
  
  public T Deserialise<T>() => Json.Deserialize<T>(Data) ?? throw new Exception();
  
  public record Dto : IDto<StagedEntity> {
    public Guid? Id { get; init; }
    public string? CorrelationId { get; init; }
    public string? System { get; init; }
    public string? SystemEntityTypeName { get; init; }
    public DateTime? DateStaged { get; init; }
    public string? Data { get; init; }
    public string? StagedEntityChecksum { get; init; }
    public DateTime? DatePromoted { get; set; }
    public string? IgnoreReason { get; set; }
    
    public Dto() {}
    
    internal Dto(Guid? id, string system, string obj, DateTime? staged, string? data, string? corrid, string? checksum, DateTime? promoted = null, string? ignoreres = null) {
      Id = id;
      System = system;
      SystemEntityTypeName = obj;
      DateStaged = staged;
      Data = data;
      CorrelationId = corrid;
      StagedEntityChecksum = checksum;
      DatePromoted = promoted;
      IgnoreReason = ignoreres;
    }
    
    public StagedEntity ToBase() => new(
        Id ?? throw new ArgumentNullException(nameof(Id)),
        new(System ?? throw new ArgumentNullException(nameof(System))),
        new(SystemEntityTypeName ?? throw new ArgumentNullException(nameof(SystemEntityTypeName))),
        DateStaged ?? throw new ArgumentNullException(nameof(DateStaged)),
        new(Data ?? throw new ArgumentNullException(nameof(Data))),
        new(CorrelationId ?? throw new ArgumentNullException(nameof(CorrelationId))),
        new(StagedEntityChecksum ?? throw new ArgumentNullException(nameof(StagedEntityChecksum)))) {
      IgnoreReason = String.IsNullOrWhiteSpace(IgnoreReason) ? null : IgnoreReason.Trim(),
      DatePromoted = DatePromoted
    };
  }
}

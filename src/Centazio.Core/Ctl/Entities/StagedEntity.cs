using System.Text.Json;
using Centazio.Core.Checksum;
using Centazio.Core.Write;

namespace Centazio.Core.Ctl.Entities;

public sealed record StagedEntity {
  
  public static StagedEntity Create(SystemName source, SystemEntityType obj, DateTime staged, ValidString data, StagedEntityChecksum checksum) => new(Guid.CreateVersion7(), source, obj, staged, data, checksum);
  
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  public StagedEntity Ignore(string reason) => this with { IgnoreReason = !String.IsNullOrWhiteSpace(reason.Trim()) ? reason.Trim() : throw new ArgumentNullException(nameof(reason)) };
  
  internal StagedEntity(Guid id, SystemName system, SystemEntityType obj, DateTime staged, ValidString data, StagedEntityChecksum checksum) {
    Id = id;
    SourceSystem = system;
    Object = obj;
    DateStaged = staged;
    Data = data;
    StagedEntityChecksum = checksum;
  }
  
  public Guid Id { get; }
  public SystemName SourceSystem { get; }
  public SystemEntityType Object { get; }
  public DateTime DateStaged { get; }
  public ValidString Data { get; internal init; }
  public StagedEntityChecksum StagedEntityChecksum { get; }
  public string? IgnoreReason { get; internal init; }
  
  public DateTime? DatePromoted { get; internal init; }
  
  public T Deserialise<T>() => JsonSerializer.Deserialize<T>(Data) ?? throw new Exception();

  public record Dto {
    public Guid? Id { get; init; }
    public string? SourceSystem { get; init; }
    public string? Object { get; init; }
    public DateTime? DateStaged { get; init; }
    public string? Data { get; init; }
    public string? Checksum { get; init; }
    public DateTime? DatePromoted { get; init; }
    public string? IgnoreReason { get; init; }
    
    public Dto() {}
    
    internal Dto(Guid? id, string syste, string obj, DateTime? staged, string? data, string? checksum, DateTime? promoted = null, string? ignoreres = null) {
      Id = id;
      SourceSystem = syste;
      Object = obj;
      DateStaged = staged;
      Data = data;
      Checksum = checksum;
      DatePromoted = promoted;
      IgnoreReason = ignoreres;
    }
    
    public static explicit operator StagedEntity(Dto dto) => new(
        dto.Id ?? throw new ArgumentNullException(nameof(Id)),
        dto.SourceSystem ?? throw new ArgumentNullException(nameof(SourceSystem)),
        new(dto.Object ?? throw new ArgumentNullException(nameof(Object))),
        dto.DateStaged ?? throw new ArgumentNullException(nameof(DateStaged)),
        dto.Data ?? throw new ArgumentNullException(nameof(Data)),
        new(dto.Checksum ?? throw new ArgumentNullException(nameof(Checksum)))) {
      IgnoreReason = String.IsNullOrWhiteSpace(dto.IgnoreReason) ? null : dto.IgnoreReason.Trim(),
      DatePromoted = dto.DatePromoted
    };
    
    public static explicit operator Dto(StagedEntity se) => new() {
      Id = se.Id,
      SourceSystem = se.SourceSystem.Value,
      Object = se.Object.Value,
      DateStaged = se.DateStaged,
      Data = se.Data.Value,
      Checksum = se.StagedEntityChecksum.Value,
      DatePromoted = se.DatePromoted,
      IgnoreReason = se.IgnoreReason
    };
  }
}

public static class StagedEntityListExtensions {
  public static List<Containers.StagedSys> Deserialise<E>(this List<StagedEntity> lst) where E : ISystemEntity => 
      lst.Select(e => new Containers.StagedSys(e, e.Deserialise<E>())).ToList(); 
}
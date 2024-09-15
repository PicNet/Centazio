﻿namespace Centazio.Core.Ctl.Entities;

public sealed record StagedEntity {
  
  public static StagedEntity Create(SystemName source, ObjectName obj, DateTime staged, ValidString data, ValidString checksum) => new(Guid.CreateVersion7(), source, obj, staged, data, checksum);
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  public StagedEntity Ignore(string reason) => this with { IgnoreReason = !String.IsNullOrWhiteSpace(reason.Trim()) ? reason.Trim() : throw new ArgumentNullException(nameof(reason)) };
  
  private StagedEntity(Guid Id, SystemName SourceSystem, ObjectName Object, DateTime DateStaged, ValidString Data, ValidString Checksum) {
    this.Id = Id;
    this.SourceSystem = SourceSystem;
    this.Object = Object;
    this.DateStaged = DateStaged;
    this.Data = Data;
    this.Checksum = Checksum;
  }
  
  public Guid Id { get; }
  public SystemName SourceSystem { get; }
  public ObjectName Object { get; }
  public DateTime DateStaged { get; }
  public ValidString Data { get; }
  public ValidString Checksum { get; }
  public string? IgnoreReason { get; private init; }
  
  public DateTime? DatePromoted { get; private init; }
  
  
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
        dto.Object ?? throw new ArgumentNullException(nameof(Object)),
        dto.DateStaged ?? throw new ArgumentNullException(nameof(DateStaged)),
        dto.Data ?? throw new ArgumentNullException(nameof(Data)),
        dto.Checksum ?? throw new ArgumentNullException(nameof(Checksum))) {
      IgnoreReason = String.IsNullOrWhiteSpace(dto.IgnoreReason) ? null : dto.IgnoreReason.Trim(),
      DatePromoted = dto.DatePromoted
    };
    
    public static explicit operator Dto(StagedEntity se) => new() {
      Id = se.Id,
      SourceSystem = se.SourceSystem.Value,
      Object = se.Object.Value,
      DateStaged = se.DateStaged,
      Data = se.Data.Value,
      Checksum = se.Checksum.Value,
      DatePromoted = se.DatePromoted,
      IgnoreReason = se.IgnoreReason
    };
  }
}
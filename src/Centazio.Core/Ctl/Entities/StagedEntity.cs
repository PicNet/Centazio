using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public static class StagedEntityEnumerableExtensions {
  public static List<E> ToSysEnt<E>(this List<StagedEntity> staged) where E : ISystemEntity => staged.Select(s => s.Deserialise<E>()).ToList();
  
  public static List<Containers.StagedSys> ToStagedSys<E>(this List<StagedEntity> staged) where E : ISystemEntity => staged.Select(s => {
    var sysent = s.Deserialise<E>();
    return new Containers.StagedSys(s, sysent);
  }).ToList();
  
  public static List<Containers.StagedSysOptionalCore> ToStagedSysCore<E>(this List<StagedEntity> staged, Func<E, ICoreEntity> ToCore) where E : ISystemEntity => staged.Select(s => {
    var sysent = s.Deserialise<E>();
    return new Containers.StagedSysOptionalCore(s, sysent, ToCore(sysent));
  }).ToList();
  
  public static List<Containers.StagedSys> Deserialise<E>(this List<StagedEntity> lst) where E : ISystemEntity => 
      lst.Select(e => new Containers.StagedSys(e, e.Deserialise<E>())).ToList();
}

public sealed record StagedEntity {
  
  public static StagedEntity Create(SystemName system, SystemEntityType systype, DateTime staged, ValidString data, StagedEntityChecksum checksum) => new(Guid.CreateVersion7(), system, systype, staged, data, checksum);
  
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  public StagedEntity Ignore(string reason) => this with { IgnoreReason = !String.IsNullOrWhiteSpace(reason.Trim()) ? reason.Trim() : throw new ArgumentNullException(nameof(reason)) };
  
  internal StagedEntity(Guid id, SystemName system, SystemEntityType systype, DateTime staged, ValidString data, StagedEntityChecksum checksum) {
    Id = id;
    System = system;
    SystemEntityType = systype;
    DateStaged = staged;
    Data = data;
    StagedEntityChecksum = checksum;
  }
  
  public Guid Id { get; }
  public SystemName System { get; }
  public SystemEntityType SystemEntityType { get; }
  public DateTime DateStaged { get; }
  public ValidString Data { get; internal init; }
  public StagedEntityChecksum StagedEntityChecksum { get; }
  public string? IgnoreReason { get; internal init; }
  
  public DateTime? DatePromoted { get; internal init; }
  
  public T Deserialise<T>() => Json.Deserialize<T>(Data) ?? throw new Exception();

  public record Dto {
    public Guid? Id { get; init; }
    public string? System { get; init; }
    public string? SystemEntityType { get; init; }
    public DateTime? DateStaged { get; init; }
    public string? Data { get; init; }
    public string? StagedEntityChecksum { get; init; }
    public DateTime? DatePromoted { get; init; }
    public string? IgnoreReason { get; init; }
    
    public Dto() {}
    
    internal Dto(Guid? id, string syste, string obj, DateTime? staged, string? data, string? checksum, DateTime? promoted = null, string? ignoreres = null) {
      Id = id;
      System = syste;
      SystemEntityType = obj;
      DateStaged = staged;
      Data = data;
      StagedEntityChecksum = checksum;
      DatePromoted = promoted;
      IgnoreReason = ignoreres;
    }
    
    public static explicit operator StagedEntity(Dto dto) => new(
        dto.Id ?? throw new ArgumentNullException(nameof(Id)),
        dto.System ?? throw new ArgumentNullException(nameof(System)),
        new(dto.SystemEntityType ?? throw new ArgumentNullException(nameof(SystemEntityType))),
        dto.DateStaged ?? throw new ArgumentNullException(nameof(DateStaged)),
        dto.Data ?? throw new ArgumentNullException(nameof(Data)),
        new(dto.StagedEntityChecksum ?? throw new ArgumentNullException(nameof(StagedEntityChecksum)))) {
      IgnoreReason = String.IsNullOrWhiteSpace(dto.IgnoreReason) ? null : dto.IgnoreReason.Trim(),
      DatePromoted = dto.DatePromoted
    };
    
    public static explicit operator Dto(StagedEntity se) => new() {
      Id = se.Id,
      System = se.System.Value,
      SystemEntityType = se.SystemEntityType.Value,
      DateStaged = se.DateStaged,
      Data = se.Data.Value,
      StagedEntityChecksum = se.StagedEntityChecksum.Value,
      DatePromoted = se.DatePromoted,
      IgnoreReason = se.IgnoreReason
    };
  }
}

using System.ComponentModel.DataAnnotations;

namespace Centazio.Core.Ctl.Entities;

public enum EChangeType { Create, Update } 

public record EntityChange {
  
  public CoreEntityTypeName CoreEntityTypeName { get; }
  public CoreEntityId CoreId { get; }
  public SystemName System { get; }
  public SystemEntityTypeName SystemEntityTypeName { get; }
  public SystemEntityId SystemId { get; }
  public DateTime ChangeDate { get; }
  public EChangeType ChangeType { get; }
  [MaxLength(4000)] public string ChangeDetails { get; }
  
  private EntityChange(CoreEntityTypeName coretype, CoreEntityId coreid, SystemName system, SystemEntityTypeName systype, SystemEntityId sysid, DateTime changedate, EChangeType changetype, string changedetails) {
    CoreEntityTypeName = coretype;
    CoreId = coreid;
    System = system;
    SystemEntityTypeName = systype;
    SystemId = sysid;
    ChangeDate = changedate;
    ChangeType = changetype;
    ChangeDetails = changedetails;
  }

  public static EntityChange Create(CoreEntityTypeName coretype, CoreEntityId coreid, SystemName system, SystemEntityTypeName systype, SystemEntityId sysid, ICoreEntity? old, ICoreEntity @new) =>
      new (coretype, coreid, system, systype, sysid, UtcDate.UtcNow, old is null ? EChangeType.Create : EChangeType.Update, GetChangesStr(old, @new));

  private static string GetChangesStr(ICoreEntity? old, ICoreEntity @new) => 
      old is null ? String.Empty : Json.Serialize(GetChanges(old, @new));

  private static Dictionary<string, FieldPairJson> GetChanges(ICoreEntity old, ICoreEntity @new) => 
      old.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
          .Select(p => new FieldChange(p.Name, p.GetValue(old)?.ToString() ?? String.Empty, p.GetValue(@new)?.ToString() ?? String.Empty))
          .Where(c => c.IsChange())
          .ToDictionary(c => c.FieldName, c => c.ToPair());
  
  record FieldChange(string FieldName, string OldValue, string NewValue) {
    public bool IsChange() => OldValue != NewValue;
    public FieldPairJson ToPair() => new(OldValue, NewValue);
  }

  // ReSharper disable NotAccessedPositionalProperty.Local
  record FieldPairJson(string old, string @new);
}


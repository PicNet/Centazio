using System.ComponentModel.DataAnnotations;
using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;

namespace Centazio.Test.Lib;

[IgnoreNamingConventions] 
public static class Constants {
  public static readonly SystemName System1Name = new("CRM");
  public static readonly SystemName System2Name = new("FIN");
  public static readonly SystemEntityId Sys1Id1 = new("S1.1");
  public static readonly SystemEntityId Sys1Id2 = new("S1.2");
  public static readonly SystemEntityTypeName SystemEntityName = new(nameof(SystemEntityTypeName));
  public static readonly CoreEntityTypeName CoreEntityName = CoreEntityTypeName.From<CoreEntity>();
  public static readonly CoreEntityTypeName CoreEntityName2 = CoreEntityTypeName.From<CoreEntity2>();
  public static readonly CoreEntityId CoreE1Id1 = new("C1.1");
  public static readonly CoreEntityId CoreE1Id2 = new("C1.2");
}

public record System1Entity(Guid Sys1EntityId, string FirstName, string LastName, DateOnly DateOfBirth, DateTime LastUpdatedDate) : ISystemEntity {

  public SystemEntityId SystemId => new(Sys1EntityId.ToString());
  public string DisplayName => $"{FirstName} {LastName}({Sys1EntityId})";
  public object GetChecksumSubset() => new { FirstName, LastName, DateOfBirth };
  public CoreEntity ToCoreEntity() => new(new(SystemId.Value), FirstName, LastName, DateOfBirth);
}

public record CoreEntity(CoreEntityId CoreId, string FirstName, string LastName, DateOnly DateOfBirth) : ICoreEntity {

  public CoreEntityId CoreId { get; set; } = CoreId;
  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  [MaxLength(64)] public string FirstName { get; init; } = FirstName;
  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  [MaxLength(64)] public string LastName { get; init; } = LastName;
  
  public string DisplayName => $"{FirstName} {LastName}";
  
  public object GetChecksumSubset() => new { FirstName, LastName, DateOfBirth };

  public record Dto : ICoreEntityDto<CoreEntity> {
    public string CoreId { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    
    public CoreEntity ToBase() => new(
        new(CoreId ?? throw new ArgumentNullException(nameof(CoreId))),
        FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
        LastName ?? throw new ArgumentNullException(nameof(LastName)),
        DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)));
  }
}

public record CoreEntity2(CoreEntityId CoreId, DateTime DateUpdated) : ICoreEntity {
  public CoreEntityId CoreId { get; set; } = CoreId;
  
  public string DisplayName { get; } = CoreId;
  
  public object GetChecksumSubset() => new { Id = CoreId };
}

public record TestSettingsRaw {
  public List<string>? SecretsFolders { get; init; }
  
  public static explicit operator TestSettings(TestSettingsRaw raw) => new(
      raw.SecretsFolders ?? throw new ArgumentNullException(nameof(SecretsFolders)));
}

public record TestSettings(List<string> SecretsFolders) {
  public string GetSecretsFolder() => FsUtils.FindFirstValidDirectory(SecretsFolders);
}

public record TestSecretsRaw {
  public string? AWS_KEY { get; init; }
  public string? AWS_SECRET { get; init; }
  public string? SQL_CONN_STR { get; init; }
  
  public static explicit operator TestSecrets(TestSecretsRaw raw) => new(
      raw.AWS_KEY ?? throw new ArgumentNullException(nameof(AWS_KEY)),
      raw.AWS_SECRET ?? throw new ArgumentNullException(nameof(AWS_SECRET)),
      raw.SQL_CONN_STR ?? throw new ArgumentNullException(nameof(SQL_CONN_STR)));
}
public record TestSecrets(string AWS_KEY, string AWS_SECRET, string SQL_CONN_STR);
﻿using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib;

public static class Constants {
  public static readonly SystemName System1Name = new("CRM");
  public static readonly SystemName System2Name = new("FIN");
  public static readonly ExternalEntityType ExternalEntityName = new(nameof(ExternalEntityType));
  public static readonly CoreEntityType CoreEntityName = CoreEntityType.From<CoreEntity>();
  public static readonly CoreEntityType CoreEntityName2 = CoreEntityType.From<CoreEntity2>();
}

public record System1Entity(Guid Id, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated);

public record CoreEntity(string Id, string Checksum, string FirstName, string LastName, DateOnly DateOfBirth, DateTime DateUpdated) : ICoreEntity {

  public string Id { get; set; } = Id;
  public string SourceId { get; init; } = Id;
  public string SourceSystem { get; } = Constants.System1Name;
  public string LastUpdateSystem { get; }  = Constants.System1Name;
  public DateTime DateCreated { get; } = DateUpdated;
  public DateTime SourceSystemDateUpdated => DateUpdated;
  public string DisplayName => $"{FirstName} {LastName}";

  public record Dto {
    public string? Id { get; init; }
    public string? Checksum { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public DateTime? DateUpdated { get; init; } 
    public string? SourceSystem { get; init; } 
    public DateTime? DateCreated { get; init; } 
    public DateTime? SourceSystemDateUpdated { get; init; }
    
    public static explicit operator CoreEntity(Dto raw) => new(
        raw.Id ?? throw new ArgumentNullException(nameof(Id)),
        raw.Checksum ?? "",
        raw.FirstName ?? throw new ArgumentNullException(nameof(FirstName)),
        raw.LastName ?? throw new ArgumentNullException(nameof(LastName)),
        raw.DateOfBirth ?? throw new ArgumentNullException(nameof(DateOfBirth)),
        raw.DateUpdated ?? UtcDate.UtcNow);
  }
}

public record CoreEntity2(string Id, string Checksum, DateTime DateUpdated) : ICoreEntity {
  public string Id { get; set; } = Id;
  public string SourceId { get; init; } = Id;
  public string SourceSystem { get; } = Constants.System2Name;
  public string LastUpdateSystem { get; } = Constants.System2Name;
  public DateTime DateCreated { get; } = DateUpdated;
  public DateTime SourceSystemDateUpdated => DateUpdated;
  
  public string DisplayName { get; } = Id;
}

public record TestSettingsRaw {
  public string? SecretsFolder { get; init; }
  
  public static explicit operator TestSettings(TestSettingsRaw raw) => new(
      raw.SecretsFolder ?? throw new ArgumentNullException(nameof(SecretsFolder)));
}

public record TestSettings(string SecretsFolder);

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
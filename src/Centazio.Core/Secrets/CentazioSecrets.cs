using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Centazio.Core.Secrets;

/// <summary>
/// The pattern used below is as follows:
///     `[NotNull] public string? AWS_KEY { get => CheckOptField(field); init; }`
/// `[NotNull]` tells the client code that the property returned will be a not null `string`
/// `string?` tells the `ISecretsLoader` that the property does not have to be specified
/// `get => CheckOptField(field);` will check that if the property is accessed, then it must have a value
/// `init;` used by the `ISecretsLoader` to set the initial value
///
/// The purpose of this is that we do not need to supply all of these secrets if they are not going to be used.  For
/// instance, if we are hosting in AWS then the Azure secrets are redundant.  However, if we try to access a secret
/// then we validate that it is available at run time.
/// </summary>
public record CentazioSecrets {
  
  [NotNull] public string? AWS_KEY { get => CheckOptField(field); init; } 
  [NotNull] public string? AWS_SECRET { get => CheckOptField(field); init; }
  
  [NotNull] public string? AZ_TENANT_ID { get => CheckOptField(field); init; } 
  [NotNull] public string? AZ_CLIENT_ID { get => CheckOptField(field); init; }
  [NotNull] public string? AZ_SECRET_ID { get => CheckOptField(field); init; }
  [NotNull] public string? AZ_SUBSCRIPTION_ID { get => CheckOptField(field); init; }
  [NotNull] public string? AZ_BLOB_STORAGE_ENDPOINT { get => CheckOptField(field); init; }
  [NotNull] public string? AZ_APP_INSIGHT_CONNECTION_STRING { get => CheckOptField(field); init; }
  
  [NotNull] public string? SQL_CONN_STR { get => CheckOptField(field); init; }
  
  [NotNull] public string? NUGET_API_KEY { get => CheckOptField(field); init; }
  
  protected string CheckOptField(string? value, [CallerMemberName] string? prop = null) => 
      value ?? throw new Exception($"secret '{prop}' was not supplied");

}
using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public enum ESecretsProviderType
{
  File,
  Aws,
  Az
}

public interface ISecretsFactory {
  Task<T> LoadSecrets<T>(CentazioSettings settings, params List<string> environments);
}
using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public enum ESecretsProviderType
{
  File,
  Aws
}

public interface ISecretsFactory {
  Task<T> LoadSecrets<T>(CentazioSettings settings, params string[] environments);
}
using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public enum ESecretsProviderType
{
  File,
  Aws,
  Az,
  EnvironmentVariable
}

public interface ISecretsFactory {

}
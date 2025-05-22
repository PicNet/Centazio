using Centazio.Core.Settings;

namespace Centazio.Core.Secrets;

public interface ISecretsLoaderFactory
{
  ISecretsLoader CreateSecretsLoader(Provider provider, CentazioSettings settings);
  void RegisterProvider(Provider provider, Func<CentazioSettings, ISecretsLoader> factory);
}

public enum Provider {
  File,
  Aws,
  Azure
}
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Providers.Aws.Secrets;

public class AwsSecretsFactory : ISecretsFactory
{
  public async Task<T> LoadSecrets<T>(CentazioSettings settings, params string[] environments)
  {
    if (settings.AwsSettings == null) throw new ArgumentNullException(nameof(settings.AwsSettings));
            
    var loader = new AwsSecretsLoader(settings.AwsSettings);
    return await loader.Load<T>(environments.ToList());
  }
}

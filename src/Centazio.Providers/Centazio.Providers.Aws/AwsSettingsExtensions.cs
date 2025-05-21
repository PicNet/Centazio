using Amazon;
using Centazio.Core.Settings;

namespace Centazio.Providers.Aws;

public static class AwsSettingsExtensions {
  public static RegionEndpoint GetRegionEndpoint(this AwsSettings settings) => RegionEndpoint.GetBySystemName(settings.Region); 
}
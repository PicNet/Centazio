using Centazio.Core.Secrets;

namespace {{ it.Namespace }};

/// <summary>
/// Add all your application specific secrets to this file.  The general pattern you should follow is:
/// `public string SETTING_NAME { get; init; } = null!;`
/// Where any `string` value will be validated and trimmed automatically.  If missing an error will be thrown when the secrets are loaded.
/// If the secret is optional then mark it with `string?`, i.e. `public string? OPTIONAL_SETTING_NAME { get; init; };`
/// Note: Do not add a constructor, and `init` is required for proper deserialisation.
/// </summary>
public record Secrets : CentazioSecrets {
  public string ADDITIONAL_SAMPLE_SECRET { get; init; } = null!;
  public string? ADDITIONAL_OPTIONAL_SAMPLE_SECRET { get; init; }
}
using Centazio.Core.Secrets;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

public abstract class BaseSecretsLoaderTests {

  protected abstract Task PrepareTestEnvironment(string environment, Dictionary<string, string> contents);
  protected abstract Task<ISecretsLoader> GetSecretsLoader();
  
  private const string FULL_CONTENT = @"SETTING1=VALUE1;
SETTING2=VALUE 2 with spaces 
# line with comments ignored
SETTING3_NUMBER=123
SETTING4=trailing space with semmi ;
SETTING5=val;with;semmis;
SETTING6=val=with=equals
SETTING7=val with # should not ignore";
  
  private ISecretsLoader loader = null!;
  
  [SetUp] public async Task SetUp() => 
      loader = await GetSecretsLoader();

  [Test] public async Task Test_loading_secrets_from_provider() {
    await PrepareTestEnvironment("testing", SecretsLoaderUtils.SplitFlatContentIntoSecretsDict(FULL_CONTENT));
    var loaded = await loader.Load<TestSettingsObj>("testing"); 
    Assert.That(loaded, Is.EqualTo(TestSettingsObj.Create("VALUE1;",
            "VALUE 2 with spaces",
            123,
            "trailing space with semmi ;",
            "val;with;semmis;",
            "val=with=equals",
            "val with # should not ignore")));
  }

  [Test] public virtual async Task Test_overwriting_secrets() {
    await PrepareTestEnvironment("testing", SecretsLoaderUtils.SplitFlatContentIntoSecretsDict(FULL_CONTENT));
    await PrepareTestEnvironment("overwrite", SecretsLoaderUtils.SplitFlatContentIntoSecretsDict("SETTING4=overwritten"));
    var loaded = await loader.Load<TestSettingsObj>("testing", "overwrite"); 
    Assert.That(loaded, Is.EqualTo(TestSettingsObj.Create("VALUE1;",
          "VALUE 2 with spaces",
          123,
          "overwritten",
          "val;with;semmis;",
          "val=with=equals",
          "val with # should not ignore")));
  }
  
  [Test] public virtual async Task Test_missing_required_field_does_not_validate() {
    var withmissing7 = FULL_CONTENT.Substring(0, FULL_CONTENT.IndexOf(nameof(TestSettingsObj.SETTING7), StringComparison.Ordinal));
    await PrepareTestEnvironment("missing-required", SecretsLoaderUtils.SplitFlatContentIntoSecretsDict(withmissing7));
    Assert.ThrowsAsync<Exception>(async () => await loader.Load<TestSettingsObj>("missing-required"));
  }
  
  [Test] public virtual async Task Test_optional_property_is_loaded_when_provided() {
    var withoptional = FULL_CONTENT + "\nSETTING8=settings_value";
    await PrepareTestEnvironment("withoptional", SecretsLoaderUtils.SplitFlatContentIntoSecretsDict(withoptional));
    var loaded = await loader.Load<TestSettingsObj>("withoptional");
    Assert.That(loaded, Is.EqualTo(TestSettingsObj.Create("VALUE1;",
            "VALUE 2 with spaces",
            123,
            "trailing space with semmi ;",
            "val;with;semmis;",
            "val=with=equals",
            "val with # should not ignore",
            "settings_value")));
  }
  
  [Test] public virtual async Task Test_optional_property_throws_error_if_accessed_and_not_supplied() {
    await PrepareTestEnvironment("testing", SecretsLoaderUtils.SplitFlatContentIntoSecretsDict(FULL_CONTENT));
    var loaded = await loader.Load<TestSettingsObj>("testing");
    
    Assert.That(loaded, Is.EqualTo(TestSettingsObj.Create("VALUE1;",
            "VALUE 2 with spaces",
            123,
            "trailing space with semmi ;",
            "val;with;semmis;",
            "val=with=equals",
            "val with # should not ignore")));
    Assert.Throws<Exception>(() => _ = loaded.SETTING8);
  }
  
  // ReSharper disable UnusedAutoPropertyAccessor.Global
  protected record TestSettingsObj {

    public string SETTING1 { get; init; } = null!;
    public string SETTING2 { get; init; } = null!;
    public int SETTING3_NUMBER { get; init; }
    public string SETTING4 { get; init; } = null!;
    public string SETTING5 { get; init; } = null!;
    public string SETTING6 { get; init; } = null!;
    public string SETTING7 { get; init; } = null!;
    public string? SETTING8 { 
      get => field ?? throw new Exception($"secret '{nameof(SETTING8)}' was not supplied"); 
      init; 
    }

    public static TestSettingsObj Create(string SETTING1, string SETTING2, int SETTING3_NUMBER, string SETTING4, string SETTING5, string SETTING6, string SETTING7, string? SETTING8 = null) => new() {
      SETTING1 = SETTING1,
      SETTING2 = SETTING2,
      SETTING3_NUMBER = SETTING3_NUMBER,
      SETTING4 = SETTING4,
      SETTING5 = SETTING5,
      SETTING6 = SETTING6,
      SETTING7 = SETTING7,
      SETTING8 = SETTING8
    };
  }
}
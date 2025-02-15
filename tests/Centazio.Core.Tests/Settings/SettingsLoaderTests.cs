using Centazio.Core.Settings;

namespace Centazio.Core.Tests.Settings;

public class SettingsLoaderTests {

  private const string test_settings_json = @"{ ""FileForTestingSettingsLoader"": ""Testing content"", ""OverridableSetting"": ""To be overriden"", ""EmptySetting"": """", ""MissingSetting"": null }";
  private const string test_settings_env_json = @"{ ""OverridableSetting"": ""Overriden"", ""EmptySetting"": ""No longer empty"", ""MissingSetting"": ""No longer missing"" }";
  
  [TearDown] public void TearDown() {}
  
  [Test] public void Test_loading_of_settings_from_dir_hierarchy() {
    TestSettings(CreateLoadAndDeleteSettings(".", String.Empty));
    TestSettings(CreateLoadAndDeleteSettings("..", String.Empty));
    TestSettings(CreateLoadAndDeleteSettings("../..", String.Empty));
    
    void TestSettings(TestSettingsObj loaded) => Assert.That(loaded, Is.EqualTo(new TestSettingsObj("Testing content", "To be overriden", String.Empty, String.Empty)));
  }
  
  [Test] public void Test_loading_of_settings_with_overriding_environment_file() {
    TestSettings(CreateLoadAndDeleteSettings(".", "test"));
    TestSettings(CreateLoadAndDeleteSettings("..", "test"));
    TestSettings(CreateLoadAndDeleteSettings("../..", "test"));
                
    void TestSettings(TestSettingsObj loaded) => Assert.That(loaded, Is.EqualTo(new TestSettingsObj("Testing content", "Overriden", "No longer empty", "No longer missing")));
  }
  
  [Test] public void Test_settings_loader_nullable_but_innacessible_properties() {
    var settings = F.Settings();
    Assert.Throws<CentazioSettings.SettingsSectionMissingException>(() => { _ = settings.AwsSettings; });
  }
  
  [Test] public void Test_parsing_commands_with_no_macros() {
    var settings = F.Settings();
    var cmd = nameof(Test_parsing_commands_with_no_macros).Replace('_', ' ');
    
    Assert.That(settings.Parse(cmd), Is.EqualTo(cmd));
  }
  
  [Test] public void Test_parsing_commands_with_settings_macros() {
    var settings = F.Settings();
    var cmd = "command with settings [settings.AzureSettings.Region] twice [settings.AzureSettings.Region]";
    var exp = cmd.Replace("[settings.AzureSettings.Region]", settings.AzureSettings.Region);
    
    Assert.That(exp, Does.Not.Contain('['));
    Assert.That(settings.Parse(cmd), Is.EqualTo(exp));
  }
  
  [Test] public void Test_parsing_commands_with_args_macros() {
    var settings = F.Settings();
    var cmd = "command with settings [Key1] twice [Key1] [Key2]";
    var exp = cmd.Replace("[Key1]", "Value1").Replace("[Key2]", "Value2");
    
    Assert.That(exp, Does.Not.Contain('['));
    Assert.That(settings.Parse(cmd, new { Key1="Value1", Key2="Value2"}), Is.EqualTo(exp));
  }
  
  [Test] public void Test_parsing_commands_with_secrets_macros() {
    var (settings, secrets) = (F.Settings(), F.Secrets());
    var cmd = "command with secret [secrets.AWS_REGION]";
    var exp = cmd.Replace("[secrets.AWS_REGION]", secrets.AWS_REGION);
    
    Assert.That(exp, Does.Not.Contain('['));
    Assert.That(settings.Parse(cmd, secrets: secrets), Is.EqualTo(exp));
  }
  
  private TestSettingsObj CreateLoadAndDeleteSettings(string dir, string environment) {
    try {
      File.WriteAllText(Path.Combine(dir, "test_settings.json"), test_settings_json);
      File.WriteAllText(Path.Combine(dir, $"test_settings.{environment}.json"), test_settings_env_json);
      return (TestSettingsObj) new SettingsLoader("test_settings.json").Load<TestSettingsObjRaw>(environment); 
    } finally { 
      File.Delete(Path.Combine(dir, "test_settings.json"));
      File.Delete(Path.Combine(dir, $"test_settings.{environment}.json"));
    }
  }
}


internal record TestSettingsObjRaw {
  public string? FileForTestingSettingsLoader { get; init; }
  public string? OverridableSetting { get; init; }
  public string? EmptySetting  { get; init; }
  public string? MissingSetting { get; init; }
  
  public static explicit operator TestSettingsObj(TestSettingsObjRaw raw) => new(
      raw.FileForTestingSettingsLoader ?? throw new ArgumentNullException(nameof(FileForTestingSettingsLoader)),
      raw.OverridableSetting ?? throw new ArgumentNullException(nameof(OverridableSetting)),
      raw.EmptySetting ?? throw new ArgumentNullException(nameof(EmptySetting)),
      raw.MissingSetting);
}

internal record TestSettingsObj(string FileForTestingSettingsLoader, string OverridableSetting, string EmptySetting, string? MissingSetting);
using System.Collections;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

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

// ReSharper disable NotAccessedPositionalProperty.Global
internal record TestSettingsObj(string FileForTestingSettingsLoader, string OverridableSetting, string EmptySetting, string? MissingSetting);
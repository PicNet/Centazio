using Centazio.Core.Settings;

namespace Centazio.Core.Tests.Settings;

public class SettingsLoaderTests {

  private const string test_settings_json = @"{ ""FileForTestingSettingsLoader"": ""Testing content"", ""OverridableSetting"": ""To be overriden"", ""EmptySetting"": """", ""MissingSetting"": null }";
  private const string test_settings_test_json = @"{ ""OverridableSetting"": ""Overriden"", ""EmptySetting"": ""No longer empty"", ""MissingSetting"": ""No longer missing"" }";
  
  [TearDown] public void TearDown() {}
  
  [Test] public void Test_loading_of_settings_from_dir_hierarchy() {
    TestSettings(CreateLoadAndDeleteSettings("."));
    TestSettings(CreateLoadAndDeleteSettings(".."));
    TestSettings(CreateLoadAndDeleteSettings("../.."));
    
    void TestSettings(TestSettingsObj loaded) => Assert.That(loaded, Is.EqualTo(new TestSettingsObj("Testing content", "To be overriden", "", "")));
  }
  
  [Test] public void Test_loading_of_settings_with_overriding_environment_file() {
    TestSettings(CreateLoadAndDeleteSettings(".", "test"));
    TestSettings(CreateLoadAndDeleteSettings("..", "test"));
    TestSettings(CreateLoadAndDeleteSettings("../..", "test"));
                
    void TestSettings(TestSettingsObj loaded) => Assert.That(loaded, Is.EqualTo(new TestSettingsObj("Testing content", "Overriden", "No longer empty", "No longer missing")));
  }
  
  private TestSettingsObj CreateLoadAndDeleteSettings(string dir, string environment="") {
    try {
      File.WriteAllText(Path.Combine(dir, "test_settings.json"), test_settings_json);
      File.WriteAllText(Path.Combine(dir, $"test_settings.{environment}.json"), test_settings_test_json);
      return new SettingsLoader<TestSettingsObj>("test_settings.json").Load(environment); 
    } finally { 
      File.Delete(Path.Combine(dir, "test_settings.json"));
      File.Delete(Path.Combine(dir, $"test_settings.{environment}.json"));
    }
  }

  // ReSharper disable NotAccessedPositionalProperty.Local
  private record TestSettingsObj(string FileForTestingSettingsLoader, string OverridableSetting, string EmptySetting, string MissingSetting) {
    public TestSettingsObj() : this("", "", "", "") {}
  }
}
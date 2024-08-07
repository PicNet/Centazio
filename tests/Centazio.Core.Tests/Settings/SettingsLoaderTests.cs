using Centazio.Core.Settings;

namespace centazio.core.tests.Settings;

public class SettingsLoaderTests {
  [Test] public void Test_loading_of_settings() {
    var settings = new SettingsLoader<TestSettingsObj>("test_settings.json").Load();
    Assert.That(settings.FileForTestingSettingsLoader, Is.EqualTo("Do not modify"));
  }
}

public record TestSettingsObj(string FileForTestingSettingsLoader);
using System.Collections;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Core.Tests.Settings;

public class SettingsLoaderTests {

  private const string test_fn_prefix = "test_settings";
  private const string test_settings_json = @"{ ""FileForTestingSettingsLoader"": ""Testing content"", ""OverridableSetting"": ""To be overriden"", ""EmptySetting"": """", ""MissingSetting"": null }";
  private const string test_settings_env_json = @"{ ""OverridableSetting"": ""Overriden"", ""EmptySetting"": ""No longer empty"", ""MissingSetting"": ""No longer missing"" }";
  
  
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
  
  [Test] public void Test_loading_in_mem_sample_settings() {
    var settings = F.Settings("in-mem");
    Assert.That(settings.StagedEntityRepository.ConnectionString, Is.EqualTo("Data Source=InMemoryCentazio;Mode=Memory;Cache=Shared"));
  }
  
  [Test] public void Test_RegisterSettingsAndRecordPropertiesAsSingletons() {
    var svcs = new TestServivesCollection();
    var registrar = new CentazioServicesRegistrar(svcs);
    var settings = SettingsLoader.RegisterSettingsHierarchy(F.Settings<CentazioSettings>(), registrar);
    var expected = new List<Type> { typeof(CentazioSettings), typeof(DefaultsSettings), typeof(StagedEntityRepositorySettings), typeof(CtlRepositorySettings), typeof(CoreStorageSettings) };
    
    Assert.That(expected.All(t => svcs.Registered.Contains(t)));
    
    Assert.That(settings.SecretsFolders, Has.Count.GreaterThan(0));
    Assert.That(settings.Defaults, Is.Not.Null);
  }
  
  private TestSettingsObj CreateLoadAndDeleteSettings(string dir, string environment) {
    dir = Path.GetFullPath(dir);
    try {
      File.WriteAllText(FsUtils.GetSolutionFilePath(dir, $"{test_fn_prefix}.json"), test_settings_json);
      File.WriteAllText(FsUtils.GetSolutionFilePath(dir, $"{test_fn_prefix}.{environment}.json"), test_settings_env_json);
      return (TestSettingsObj) new SettingsLoader(new SettingsLoaderConfig(test_fn_prefix, dir, true)).Load<TestSettingsObjRaw>(environment); 
    } finally { 
      File.Delete(FsUtils.GetSolutionFilePath(dir, $"{test_fn_prefix}.json"));
      File.Delete(FsUtils.GetSolutionFilePath(dir, $"{test_fn_prefix}.{environment}.json"));
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

public class TestServivesCollection : IServiceCollection {

  public List<Type> Registered { get; } = [];
  
  public int Count => 0;
  public bool IsReadOnly => false;
  
  public IEnumerator<ServiceDescriptor> GetEnumerator() => throw new Exception();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  
  public void Add(ServiceDescriptor item) => Registered.Add(item.ServiceType);
  public void Clear() => throw new Exception();
  public bool Contains(ServiceDescriptor item) => throw new Exception();
  public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => throw new Exception();
  public bool Remove(ServiceDescriptor item) => throw new Exception();
  public int IndexOf(ServiceDescriptor item) => throw new Exception();
  public void Insert(int index, ServiceDescriptor item) => throw new Exception();
  public void RemoveAt(int index) => throw new Exception();
  
  public ServiceDescriptor this[int index] { get => throw new Exception(); set => throw new Exception(); }

}

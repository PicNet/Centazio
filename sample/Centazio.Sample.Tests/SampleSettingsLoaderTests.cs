using System.Collections;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Sample.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.Tests;

public class SettingsLoaderTests {

  
  [Test] public async Task Test_RegisterSettingsAndRecordPropertiesAsSingletons() {
    var svcs = new TestServivesCollection();
    var registrar = new CentazioServicesRegistrar(svcs);
    var settings = SettingsLoader.RegisterSettingsHierarchy(await F.Settings<Settings>(), registrar);
    
    var expected = new List<Type> { typeof(Settings), typeof(ClickUpSettings), typeof(AppSheetSettings), typeof(StagedEntityRepositorySettings), typeof(CtlRepositorySettings), typeof(CoreStorageSettings) };
    var missings = expected.Except(svcs.Registered).ToList();
    
    Assert.That(missings, Is.Empty);
    Assert.That(settings.Defaults, Is.Not.Null);
    Assert.That(settings.ClickUp, Is.Not.Null);
    Assert.That(settings.AppSheet, Is.Not.Null);
  }

}

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

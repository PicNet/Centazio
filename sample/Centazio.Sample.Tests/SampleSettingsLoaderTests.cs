using System.Collections;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Sample.Tests;

public class SampleSettingsLoaderTests {

  
  [Test] public void Test_RegisterSettingsAndRecordPropertiesAsSingletons() {
    var svcs = new TestServivesCollection();
    SettingsLoader.RegisterSettingsAndRecordPropertiesAsSingletons(F.Settings<SampleSettings>(), svcs);
  }

}

public class TestServivesCollection : IServiceCollection {

  public IEnumerator<ServiceDescriptor> GetEnumerator() => throw new Exception();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Add(ServiceDescriptor item) {
    Console.WriteLine("ADD: " + item.ServiceType.Name);
  }
  public void Clear() {
    throw new Exception();
  }
  public bool Contains(ServiceDescriptor item) => throw new Exception();

  public void CopyTo(ServiceDescriptor[] array, int arrayIndex) {
    throw new Exception();
  }
  public bool Remove(ServiceDescriptor item) => throw new Exception();
  public int Count { get; }
  public bool IsReadOnly { get; }
  public int IndexOf(ServiceDescriptor item) => throw new Exception();

  public void Insert(int index, ServiceDescriptor item) {
    throw new Exception();
  }
  public void RemoveAt(int index) {
    throw new Exception();
  }

  public ServiceDescriptor this[int index] { get => throw new Exception(); set => throw new Exception(); }

}

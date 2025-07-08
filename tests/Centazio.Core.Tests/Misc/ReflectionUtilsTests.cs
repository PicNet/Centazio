using Centazio.Core.Stage;
using Centazio.Test.Lib;
using U = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Core.Tests.Misc;

public class ReflectionUtilsTests {
  
  public int TestProp { get; set; } = Int32.MinValue;
  
  [Test] public void Test_GetPropVal() {
    Assert.That(U.GetPropVal<int>(this, nameof(TestProp)), Is.EqualTo(Int32.MinValue));
  }
  
  [Test] public void Test_GetPropValAsString() {
    Assert.That(U.GetPropValAsString(this, nameof(TestProp)), Is.EqualTo(Int32.MinValue.ToString()));
  }
  
  [Test] public void Test_IsRecord() {
    Assert.That(U.IsRecord(GetType()), Is.False);
    Assert.That(U.IsRecord(typeof(ReflectionUtilsTests)), Is.False);
    Assert.That(U.IsRecord(typeof(SystemName)));
    Assert.That(U.IsRecord(typeof(SystemEntityId)));
    Assert.That(U.IsRecord(typeof(EntityId)));
    Assert.That(U.IsRecord(typeof(ValidString)));
  }
  
  [Test] public void Test_IsNullable() {
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.System))), Is.False);
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.Stage))), Is.False);
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.DateCreated))), Is.False);
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.DateUpdated))), Is.False);
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.Active))), Is.False);
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.Status))), Is.False);
    
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.LastStarted))), Is.True);
    Assert.That(U.IsNullable(Prop(typeof(SystemState), nameof(SystemState.LastCompleted))), Is.True);
    
    Assert.That(typeof(SystemState.Dto).GetProperties().All(U.IsNullable), Is.True);
    
    PropertyInfo Prop(Type t, string name) => t.GetProperty(name) ?? throw new Exception();
  }
  
  [Test] public void Test_IsPropJsonIgnored() {
    Assert.That(U.IsJsonIgnore(typeof(ICoreEntity), nameof(ICoreEntity.DisplayName)), Is.True);
    Assert.That(U.IsJsonIgnore(typeof(CoreEntity), nameof(CoreEntity.DisplayName)), Is.True); // inherited
    Assert.That(U.IsJsonIgnore(typeof(CoreEntity), nameof(CoreEntity.FirstName)), Is.False);
  }

  [Test] public void Test_get_inherited_attribute() {
    Assert.That(U.GetPropAttribute<MaxLength2Attribute>(typeof(SystemState), nameof(SystemState.Stage))?.Length, Is.EqualTo(32));
    Assert.That(U.GetPropAttribute<MaxLength2Attribute>(typeof(StagedEntity), nameof(StagedEntity.StagedEntityChecksum))?.Length, Is.EqualTo(64));
  }
  
  [Test] public void Test_GetAllProperties() {
    var props = U.GetAllProperties<ISystemEntity>().Select(pi => pi.Name);
    Assert.That(props, Is.EquivalentTo([nameof(ISystemEntity.DisplayName), nameof(ISystemEntity.SystemId), nameof(ISystemEntity.LastUpdatedDate), nameof(ISystemEntity.CorrelationId)]));
  }
  
  [Test] public void Test_GetAssemblyPath() {
    Assert.That(U.GetAssemblyPath("Centazio.Core").Contains(Path.Combine("Centazio.Core", "bin")));
    Assert.That(U.GetAssemblyPath("Centazio.Core.Tests").Contains(Path.Combine("Centazio.Core.Tests", "bin")));
    Assert.That(U.GetAssemblyPath("Centazio.TestFunctions").Contains(Path.Combine("Centazio.TestFunctions", "bin")));
  }
  
  [Test] public void Test_LoadAssembly() {
    var ass = U.LoadAssembly("Centazio.TestFunctions");
    Assert.That(ass.Location.Contains(Path.Combine("Centazio.TestFunctions", "bin")));
  }
  
  [Test] public async Task Test_LoadAssembliesFuzzy_with_separators() {
    var settings = await F.Settings();
    var exp = new List<string> { "Centazio.Sample.AppSheet", "Centazio.Sample.ClickUp" };
    var separators = new List<string> {", ", ",", "; ", ";", "|", "| ", " "};
    separators.ForEach(Impl);
    
    void Impl(string separator) {
      var names = U.LoadAssembliesFuzzy(String.Join(separator, exp), [settings.Defaults.GeneratedCodeFolder]).Select(a => a.GetName().Name).ToList();
      Assert.That(names, Is.EquivalentTo(exp));
    }
  }
  
  [Test] public async Task Test_LoadAssembliesFuzzy_with_single_pattern() {
    var settings = await F.Settings();
    var exp = new List<string> { "Centazio.Sample.AppSheet", "Centazio.Sample.ClickUp" };
    
    var names = U.LoadAssembliesFuzzy("Centazio.Sample.*", [settings.Defaults.GeneratedCodeFolder]).Select(a => a.GetName().Name).ToList();
    Assert.That(names, Has.Count.GreaterThan(exp.Count));
    exp.ForEach(e => Assert.That(names.IndexOf(e), Is.GreaterThanOrEqualTo(0)));
  }
  
  [Test] public async Task Test_LoadAssembliesFuzzy_with_multiple_pattern() {
    var settings = await F.Settings();
    var exp = new List<string> { "Centazio.Sample.AppSheet", "Centazio.Sample.ClickUp", "Centazio.TestFunctions" };
    
    var patterns = new List<string> { "Centazio.Sample.*", "Centazio.TestFunc*" };
    var separators = new List<string> {", ", ",", "; ", ";", "|", "| ", " "};
    separators.ForEach(Impl);
    
    void Impl(string separator) {
      var names = U.LoadAssembliesFuzzy(String.Join(separator, patterns), [settings.Defaults.GeneratedCodeFolder]).Select(a => a.GetName().Name).ToList();
      Assert.That(names, Has.Count.GreaterThan(exp.Count));
      exp.ForEach(e => Assert.That(names.IndexOf(e), Is.GreaterThanOrEqualTo(0)));
    }
  }
  
  [Test] public void Test_GetAllTypesThatImplement() {
    var integration = U.GetAllTypesThatImplement(typeof(IntegrationBase<,>), ["Centazio.TestFunctions"]).Single();
    Assert.That(integration.FullName, Is.EqualTo("Centazio.TestFunctions.TestFunctionIntegration"));
    Assert.That(integration.Assembly.Location.Contains(Path.Combine("Centazio.TestFunctions", "bin")));
  }
 
  [Test] public void Test_GetProviderAssemblies() {
    Assert.That(U.GetProviderAssemblies(), Has.Count.GreaterThanOrEqualTo(3));
  }
  
  [Test] public void Test_ParseValue() {
    Assert.That(U.ParseValue(new { Key1="Value1" }, "Key1"), Is.EqualTo("Value1"));
    Assert.That(U.ParseValue(new { Key1= new { Step2 = "Value2" } }, "Key1.Step2"), Is.EqualTo("Value2"));
  }
}
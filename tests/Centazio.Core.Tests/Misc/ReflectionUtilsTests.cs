using System.Reflection;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
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
    var props = ReflectionUtils.GetAllProperties<ISystemEntity>().Select(pi => pi.Name);
    Assert.That(props, Is.EquivalentTo(["DisplayName", "SystemId", "LastUpdatedDate"]));
  }
  
  [Test] public void Test_GetAssemblyPath() {
    Assert.That(ReflectionUtils.GetAssemblyPath("Centazio.Core").Contains("\\Centazio.Core\\bin\\"));
    Assert.That(ReflectionUtils.GetAssemblyPath("Centazio.Core.Tests").Contains("\\Centazio.Core.Tests\\bin\\"));
    Assert.That(ReflectionUtils.GetAssemblyPath("Centazio.Sample").Contains("\\Centazio.Sample\\bin\\"));
  }
  
  [Test] public void Test_LoadAssembly() {
    var ass = ReflectionUtils.LoadAssembly("Centazio.Sample");
    Assert.That(ass.Location.Contains("\\Centazio.Sample\\bin\\"));
  }
  
  [Test] public void Test_GetAllTypesThatImplement() {
    var integration = ReflectionUtils.GetAllTypesThatImplement(typeof(IntegrationBase<,>), ["Centazio.Sample"]).Single();
    Assert.That(integration.FullName, Is.EqualTo("Centazio.Sample.SampleIntegration"));
    Assert.That(integration.Assembly.Location.Contains("\\Centazio.Sample\\bin\\"));
  }
 
  [Test] public void Test_GetProviderAssemblies() {
    Assert.That(ReflectionUtils.GetProviderAssemblies(), Has.Count.GreaterThanOrEqualTo(3));
  }
  
  [Test] public void Test_ParseValue() {
    Assert.That(ReflectionUtils.ParseValue(new { Key1="Value1" }, "Key1"), Is.EqualTo("Value1"));
    Assert.That(ReflectionUtils.ParseValue(new { Key1= new { Step2 = "Value2" } }, "Key1.Step2"), Is.EqualTo("Value2"));
  }
}
using Centazio.Core.Checksum;

namespace Centazio.Core.Tests;

public class TypesTests {

  [Test] public void Test_ValidString_trims_value() {
    Assert.That(new ValidString($"\n \t{nameof(TypesTests)} \n\t\t ").Value, Is.EqualTo($"{nameof(TypesTests)}"));
    Assert.That(((ValidString) $"\n \t{nameof(TypesTests)} \n\t\t ").Value, Is.EqualTo($"{nameof(TypesTests)}"));
  }
  
  [Test] public void Test_ValidString_fails_on_null() {
    Assert.Throws<ArgumentException>(() => { _ = new ValidString(null!); });
    Assert.Throws<ArgumentException>(() => { _ = (ValidString) (string) null!; });
  }
  
  [Test] public void Test_ValidString_fails_on_empty() {
    Assert.Throws<ArgumentException>(() => { _ = new ValidString(String.Empty); });
    Assert.Throws<ArgumentException>(() => { _ = (ValidString) String.Empty; });
  }
  
  
  [Test] public void Test_ObjectName_casting_to_SystemEntityType_and_CoreEntityType() {
    Assert.Throws<Exception>(() => _ = C.SystemEntityName.ToCoreEntityTypeName);
    Assert.Throws<Exception>(() => _ = C.CoreEntityName.ToSystemEntityTypeName);
    
    Assert.That(C.SystemEntityName.ToSystemEntityTypeName.Value, Is.EqualTo(C.SystemEntityName.Value));
    Assert.That(C.CoreEntityName.ToCoreEntityTypeName.Value, Is.EqualTo(C.CoreEntityName.Value));
  }
  
  [Test] public void Test_ValidString_AllSubclasses() {
    var types = ValidString.AllSubclasses();
    var expected = new [] { typeof(ValidString), typeof(CoreEntityId),typeof(SystemEntityId), typeof(SystemName), typeof(ObjectName),typeof(SystemEntityTypeName),typeof(CoreEntityTypeName),typeof(LifecycleStage),typeof(StagedEntityChecksum),typeof(SystemEntityChecksum),typeof(CoreEntityChecksum) };
    Assert.That(expected.All(t => types.IndexOf(t) >= 0), Is.True);
  }

}
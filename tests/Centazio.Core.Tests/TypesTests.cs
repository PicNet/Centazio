namespace Centazio.Core.Tests;

public class TypesTests {

  [Test] public void Test_ValidString_trims_value() {
    Assert.That(new ValidString($"\n \t{nameof(TypesTests)} \n\t\t ").Value, Is.EqualTo($"{nameof(TypesTests)}"));
  }
  
  [Test] public void Test_ValidString_fails_on_null() {
    Assert.Throws<ArgumentException>(() => { _ = new ValidString(null!); });
    Assert.Throws<ArgumentException>(() => { _ = (ValidString) (string) null!; });
  }
  
  [Test] public void Test_ValidString_fails_on_empty() {
    Assert.Throws<ArgumentException>(() => { _ = new ValidString(""); });
    Assert.Throws<ArgumentException>(() => { _ = (ValidString) ""; });
  }
  
  [Test] public void Test_SystemName_conversions() {
    var str = nameof(TypesTests);
    
    Assert.That((SystemName) str, Is.EqualTo((SystemName) nameof(TypesTests)));
    
    Assert.That((string) new SystemName(str), Is.EqualTo(str));
    Assert.That(new SystemName(str).Value, Is.EqualTo(str));
    Assert.That(new SystemName(str), Is.EqualTo((SystemName) str));
    Assert.That(new SystemName(str), Is.EqualTo((SystemName) str));
    Assert.That(new SystemName(new ValidString(str)).Value, Is.EqualTo(str));
    Assert.That(new SystemName(new ValidString(str)), Is.EqualTo((SystemName) str));
    Assert.That(new SystemName(new ValidString(str)), Is.EqualTo((SystemName) str));
  }

}
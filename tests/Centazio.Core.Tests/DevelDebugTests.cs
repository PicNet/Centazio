namespace Centazio.Core.Tests;

public class DevelDebugTests {

  [Test] public void Test_default_write() {
    DevelDebug.WriteLine(nameof(DevelDebugTests));
  }
  
  [Test] public void Test_intercepted_write() {
    var intercepted = String.Empty;
    DevelDebug.TargetWriteLine = str => intercepted = str; 
    DevelDebug.WriteLine(nameof(DevelDebugTests));
    Assert.That(intercepted, Is.EquivalentTo(nameof(DevelDebugTests)));
  }

}
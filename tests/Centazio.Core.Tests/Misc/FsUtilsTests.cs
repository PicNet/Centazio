namespace Centazio.Core.Tests.Misc;

public class FsUtilsTests {

  [Test] public void Test_FindDirOfFile() {
    var found = FsUtils.FindFileDirectory("settings.json");
    var notfound = FsUtils.FindFileDirectory(Guid.NewGuid().ToString());
    
    Assert.That(found, Is.Not.Null);
    Assert.That(notfound, Is.Null);
  }

}
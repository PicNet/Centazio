using Centazio.Core.Secrets;

namespace centazio.core.tests.Secrets;

public class NetworkLocationEnvFileSecretsLoaderTests {

  private static readonly string CONTENTS = "SETTING1=VALUE1;\nSETTING2=VALUE 2 with spaces \n\nSETTING3_NUMBER=123\nSETTING4=trailing space with semmi ;\nSETTING5=val;with;semmis;\nSETTING6=val=with=equals";
  
  [Test] public void Test_loading_from_local() {
    Assert.That(Load("testing"), Is.EqualTo(new TestSettingsTargetObj("VALUE1;", "VALUE 2 with spaces", 123, "trailing space with semmi ;", "val;with;semmis;", "val=with=equals")));
  }
  
  private TestSettingsTargetObj Load(string env) {
    var file = $"{env}.env";
    try { 
      File.WriteAllText(file, CONTENTS);
      return new NetworkLocationEnvFileSecretsLoader<TestSettingsTargetObj>(".", env).Load();
    }
    finally { File.Delete(file); }
  }

  // ReSharper disable NotAccessedPositionalProperty.Local
  private record TestSettingsTargetObj(string SETTING1, string SETTING2, int SETTING3_NUMBER, string SETTING4, string SETTING5, string SETTING6) {
    public TestSettingsTargetObj() : this("", "", 0, "", "", "") {  }
  }
}
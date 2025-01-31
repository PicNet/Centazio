using Centazio.Core.Secrets;

namespace Centazio.Core.Tests.Secrets;

public class NetworkLocationEnvFileSecretsLoaderTests {

  private const string CONTENTS = "SETTING1=VALUE1;\nSETTING2=VALUE 2 with spaces \n\nSETTING3_NUMBER=123\nSETTING4=trailing space with semmi ;\nSETTING5=val;with;semmis;\nSETTING6=val=with=equals";

  [Test] public void Test_loading_from_local() {
    Assert.That(Load("testing"), Is.EqualTo(new TestSettingsTargetObj("VALUE1;", "VALUE 2 with spaces", 123, "trailing space with semmi ;", "val;with;semmis;", "val=with=equals")));
  }
  
  private TestSettingsTargetObj Load(string env) {
    var file = $"{env}.env";
    try { 
      File.WriteAllText(file, CONTENTS);
      return (TestSettingsTargetObj) new NetworkLocationEnvFileSecretsLoader(".").Load<TestSettingsTargetObjRaw>(env);
    }
    finally { File.Delete(file); }
  }

  private record TestSettingsTargetObjRaw {
    public string? SETTING1 { get; init; }
    public string? SETTING2 { get; init; } 
    public int? SETTING3_NUMBER { get; init; } 
    public string? SETTING4 { get; init; } 
    public string? SETTING5 { get; init; }
    public string? SETTING6 { get; init; }
    
    public static explicit operator TestSettingsTargetObj(TestSettingsTargetObjRaw raw) => new (
        raw.SETTING1 ?? throw new ArgumentNullException(nameof(SETTING1)), 
        raw.SETTING2 ?? throw new ArgumentNullException(nameof(SETTING2)), 
        raw.SETTING3_NUMBER ?? throw new ArgumentNullException(nameof(SETTING3_NUMBER)), 
        raw.SETTING4 ?? throw new ArgumentNullException(nameof(SETTING4)), 
        raw.SETTING5 ?? throw new ArgumentNullException(nameof(SETTING5)), 
        raw.SETTING6 ?? throw new ArgumentNullException(nameof(SETTING6)));
  }
  private record TestSettingsTargetObj(string SETTING1, string SETTING2, int SETTING3_NUMBER, string SETTING4, string SETTING5, string SETTING6);
}
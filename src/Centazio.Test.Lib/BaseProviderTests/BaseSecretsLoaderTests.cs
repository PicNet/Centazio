using Centazio.Core.Secrets;
using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;

// todo GT: all of these tests should set themselves up and not expect the
//    environments (local, az, aws) to have these testing secrets set up just for the tests 
public abstract class BaseSecretsLoaderTests {

  protected const string FULL_CONTENT = @"SETTING1=VALUE1;
SETTING2=VALUE 2 with spaces 
# line with comments ignored
SETTING3_NUMBER=123
SETTING4=trailing space with semmi ;
SETTING5=val;with;semmis;
SETTING6=val=with=equals
SETTING7=val with # should not ignore";

  protected abstract Task PrepareTestEnvironment(string environment, string contents);
  protected abstract Task<ISecretsLoader> GetSecretsLoader();
  
  private ISecretsLoader loader = null!;
  
  [SetUp] public async Task SetUp() => 
      loader = await GetSecretsLoader();

  [Test] public async Task Test_loading_secrets_from_provider() {
    await PrepareTestEnvironment("testing", FULL_CONTENT);
    var loaded = (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>("testing"); 
    Assert.That(loaded, Is.EqualTo(new TestSettingsTargetObj("VALUE1;",
            "VALUE 2 with spaces",
            123,
            "trailing space with semmi ;",
            "val;with;semmis;",
            "val=with=equals",
            "val with # should not ignore")));
  }

  [Test] public virtual async Task Test_overwriting_secrets() {
    await PrepareTestEnvironment("testing", FULL_CONTENT);
    await PrepareTestEnvironment("overwrite", "SETTING4=overwritten");
    var loaded = (TestSettingsTargetObj) await loader.Load<TestSettingsTargetObjRaw>("testing", "overwrite"); 
    Assert.That(loaded, Is.EqualTo(new TestSettingsTargetObj("VALUE1;",
          "VALUE 2 with spaces",
          123,
          "overwritten",
          "val;with;semmis;",
          "val=with=equals",
          "val with # should not ignore")));
  }

  // ReSharper disable once ClassNeverInstantiated.Local
  protected record TestSettingsTargetObjRaw {

    public string? SETTING1 { get; init; }
    public string? SETTING2 { get; init; }
    public int? SETTING3_NUMBER { get; init; }
    public string? SETTING4 { get; init; }
    public string? SETTING5 { get; init; }
    public string? SETTING6 { get; init; }
    public string? SETTING7 { get; init; }

    public static explicit operator TestSettingsTargetObj(TestSettingsTargetObjRaw raw) => new(raw.SETTING1 ?? throw new ArgumentNullException(nameof(SETTING1)),
        raw.SETTING2 ?? throw new ArgumentNullException(nameof(SETTING2)),
        raw.SETTING3_NUMBER ?? throw new ArgumentNullException(nameof(SETTING3_NUMBER)),
        raw.SETTING4 ?? throw new ArgumentNullException(nameof(SETTING4)),
        raw.SETTING5 ?? throw new ArgumentNullException(nameof(SETTING5)),
        raw.SETTING6 ?? throw new ArgumentNullException(nameof(SETTING6)),
        raw.SETTING7 ?? throw new ArgumentNullException(nameof(SETTING7)));
  }

  // ReSharper disable NotAccessedPositionalProperty.Local
  protected record TestSettingsTargetObj(
      string SETTING1,
      string SETTING2,
      int SETTING3_NUMBER,
      string SETTING4,
      string SETTING5,
      string SETTING6,
      string SETTING7);

}
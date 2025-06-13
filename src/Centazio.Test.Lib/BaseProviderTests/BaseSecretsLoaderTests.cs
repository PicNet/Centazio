using NUnit.Framework;

namespace Centazio.Test.Lib.BaseProviderTests;
public abstract class BaseSecretsLoaderTests {

  protected const string FULL_CONTENT = @"SETTING1=VALUE1;
SETTING2=VALUE 2 with spaces 
# line with comments ignored
SETTING3_NUMBER=123
SETTING4=trailing space with semmi ;
SETTING5=val;with;semmis;
SETTING6=val=with=equals
SETTING7=val with # should not ignore";

  protected abstract Task<TestSettingsTargetObj> Load(params (string env, string contents)[] envs);
  
  [Test] public async Task Test_loading_from_local() => Assert.That(await Load(("testing", FULL_CONTENT)),
      Is.EqualTo(new TestSettingsTargetObj("VALUE1;",
          "VALUE 2 with spaces",
          123,
          "trailing space with semmi ;",
          "val;with;semmis;",
          "val=with=equals",
          "val with # should not ignore")));

  [Test, Ignore("Overwriting secrets is not needed.")] public async Task Test_overwriting_secrets() => Assert.That(await Load(("testing", FULL_CONTENT), ("overwrite", "SETTING4=overwritten")),
      Is.EqualTo(new TestSettingsTargetObj("VALUE1;",
          "VALUE 2 with spaces",
          123,
          "overwritten",
          "val;with;semmis;",
          "val=with=equals",
          "val with # should not ignore")));

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
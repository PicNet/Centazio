using Centazio.Sample.AppSheet;

namespace Centazio.Sample.Tests.AppSheet;

public class AppSheetApiTests {

  [Test] public async Task Test_GetSheetRows() {
    var rows = await Api().GetAllTasks();
    Assert.That(rows, Is.Not.Empty);
  }
  
  private AppSheetApi Api() => new(F.Settings<SampleSettings>(), F.Secrets<SampleSecrets>());

}
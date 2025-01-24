using Centazio.Sample.GoogleSheets;

namespace Centazio.Sample.Tests.GoogleSheets;

public class GoogleSheetsApiTests {

  [Test] public async Task Test_GetSheetRows() {
    var rows = await Api().GetSheetData();
    Assert.That(rows, Is.Not.Empty);
    Assert.That(rows.First(), Is.EqualTo("Tasks"));
  }

  [Test] public async Task Test_WriteSheetsData() {
    await Api().WriteSheetData(["test1", "test2"]);
    var rows = await Api().GetSheetData();
    Assert.That(rows, Is.EquivalentTo(["Tasks", "test1", "test2"]));
  }
  
  private GoogleSheetsApi Api() => new(F.Settings<SampleSettings>());

}
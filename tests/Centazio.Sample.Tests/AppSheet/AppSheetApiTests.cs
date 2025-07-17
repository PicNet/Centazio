using Centazio.Core.Misc;
using Centazio.Sample.AppSheet;
using Centazio.Sample.Shared;

namespace Centazio.Sample.Tests.AppSheet;

public class AppSheetApiTests {

  [Test] public async Task Test_AddTasks() {
    var start = await (await GetApi()).GetAllTasks();
    var added = await (await GetApi()).AddTasks([Guid.NewGuid().ToString()]);
    var end = await (await GetApi()).GetAllTasks();
      
    Assert.That(added.Count, Is.EqualTo(1));
    Assert.That(end.Count, Is.EqualTo(start.Count + 1));
    
    await (await GetApi()).DeleteTasks(added);
    var cleanup = await (await GetApi()).GetAllTasks();
    
    Assert.That(cleanup.Count, Is.EqualTo(start.Count));
  }
  
  [Test] public async Task Test_EditTasks() {
    var start = await (await GetApi()).GetAllTasks();
    var (val1, val2) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
    var added = await (await GetApi()).AddTasks([val1]);
    var toedit = added.Single() with { Task = val2 };
    var edited = await (await GetApi()).EditTasks([toedit]);
    var end = (await (await GetApi()).GetAllTasks()).Select(Json.Deserialize<AppSheetTask>).ToList();
    
    Assert.That(end.Count, Is.EqualTo(start.Count + 1));
    Assert.That(edited.Single().Task, Is.EqualTo(val2));
    Assert.That(end.Single(t => t.RowId == toedit.RowId).Task, Is.EqualTo(val2));
    
    await (await GetApi()).DeleteTasks(added);
    var cleanup = await (await GetApi()).GetAllTasks();
    
    Assert.That(cleanup.Count, Is.EqualTo(start.Count)); 
  }
  
  private async Task<AppSheetApi> GetApi() => new((await F.Settings<Settings>()).AppSheet, await F.Secrets<Secrets>());

}
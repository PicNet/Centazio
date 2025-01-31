using Centazio.Core.Misc;
using Centazio.Sample.AppSheet;

namespace Centazio.Sample.Tests.AppSheet;

public class AppSheetApiTests {

  [Test] public async Task Test_AddTasks() {
    var start = await Api().GetAllTasks();
    var added = await Api().AddTasks([Guid.NewGuid().ToString()]);
    var end = await Api().GetAllTasks();
      
    Assert.That(added.Count, Is.EqualTo(1));
    Assert.That(end.Count, Is.EqualTo(start.Count + 1));
    
    await Api().DeleteTasks(added);
    var cleanup = await Api().GetAllTasks();
    
    Assert.That(cleanup.Count, Is.EqualTo(start.Count));
  }
  
  [Test] public async Task Test_EditTasks() {
    var start = await Api().GetAllTasks();
    var (val1, val2) = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
    var added = await Api().AddTasks([val1]);
    var toedit = added.Single() with { Task = val2 };
    var edited = await Api().EditTasks([toedit]);
    var end = (await Api().GetAllTasks()).Select(Json.Deserialize<AppSheetTask>).ToList();
    
    Assert.That(end.Count, Is.EqualTo(start.Count + 1));
    Assert.That(edited.Single().Task, Is.EqualTo(val2));
    Assert.That(end.Single(t => t.RowId == toedit.RowId).Task, Is.EqualTo(val2));
    
    await Api().DeleteTasks(added);
    var cleanup = await Api().GetAllTasks();
    
    Assert.That(cleanup.Count, Is.EqualTo(start.Count)); 
  }
  
  private AppSheetApi Api() => new(F.Settings<SampleSettings>().AppSheet, F.Secrets<SampleSecrets>());

}
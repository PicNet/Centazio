using Centazio.Core.Misc;
using Centazio.Sample.ClickUp;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpApiTests {

  // https://app.clickup.com/t/{test_task_id}
  internal static readonly string TEST_TASK_ID = "86cxvdxet";
  private readonly ClickUpApi Api = new(F.Settings<Settings>(), F.Secrets<Secrets>());
  
  [Test] public async Task Test_print_task_list() {
    var tasks = await Api.GetTasksAfter(UtcDate.UtcNow.AddYears(-10));
    DevelDebug.WriteLine("TASKS:\n\t" + String.Join("\n\t", tasks.Select(t => t.Json)));
  }
  
  [Test] public async Task Test_create_task() {
    var start = DateTime.UtcNow.AddMinutes(-1); // using DateTime.UtcNow on purpose as we want empty result set
    var first = await Api.GetTasksAfter(DateTime.UtcNow);
    var name = $"{nameof(ClickUpApiTests)}:{Guid.NewGuid()}";
    var id = await Api.CreateTask(name);
    var created = (await Api.GetTasksAfter(start))
        .Select(t => Json.Deserialize<ClickUpTask>(t.Json))
        .Single(t => t.id == id);
    await Api.DeleteTask(id);
    var afterdel = (await Api.GetTasksAfter(start))
        .Select(t => Json.Deserialize<ClickUpTask>(t.Json))
        .Count(t => t.id == id);
    
    Assert.That(first, Is.Empty);
    Assert.That(created.id, Is.EqualTo(id));
    Assert.That(created.name, Is.EqualTo(name));
    Assert.That(afterdel, Is.Zero);
  }
  
  [Test] public async Task Test_update_task() {
    var name = "Centazio Unit Test Task (do not delete): " + Guid.NewGuid();
    await Api.UpdateTask(TEST_TASK_ID, name); 
  }
  
  [Test] public async Task Test_close_task() { 
    await Api.CloseTask(TEST_TASK_ID); 
  }
  
  [Test] public async Task Test_open_task() { 
    await Api.OpenTask(TEST_TASK_ID); 
  }
  
  [Test, Ignore("Do not delete, as there is no way to get task back")] public async Task Test_delete_task() { 
    await Api.DeleteTask(TEST_TASK_ID); 
  }
}
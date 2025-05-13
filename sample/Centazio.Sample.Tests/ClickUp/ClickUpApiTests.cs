using Centazio.Core.Misc;
using Centazio.Sample.ClickUp;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpApiTests {

  // https://app.clickup.com/t/{test_task_id}
  internal static readonly string TEST_TASK_ID = "86cxvdxet";
  
  
  [Test] public async Task Test_print_task_list() {
    var tasks = await (await GetApi()).GetTasksAfter(UtcDate.UtcNow.AddYears(-10));
    DevelDebug.WriteLine("TASKS:\n\t" + String.Join("\n\t", tasks.Select(t => t.Json)));
  }
  
  [Test] public async Task Test_create_task() {
    if (Env.IsGitHubActions()) return; // flaky test, ignore in CI
    var api = await GetApi();
    var start = DateTime.UtcNow.AddMinutes(-1); // using DateTime.UtcNow on purpose as we want empty result set
    var first = await api.GetTasksAfter(DateTime.UtcNow);
    var name = $"{nameof(ClickUpApiTests)}:{Guid.NewGuid()}";
    var id = await api.CreateTask(name);
    var created = (await api.GetTasksAfter(start))
        .Select(t => Json.Deserialize<ClickUpTask>(t.Json))
        .Single(t => t.id == id);
    await api.DeleteTask(id);
    var afterdel = (await api.GetTasksAfter(start))
        .Select(t => Json.Deserialize<ClickUpTask>(t.Json))
        .Count(t => t.id == id);
    
    Assert.That(first, Is.Empty);
    Assert.That(created.id, Is.EqualTo(id));
    Assert.That(created.name, Is.EqualTo(name));
    Assert.That(afterdel, Is.Zero);
  }
  
  [Test] public async Task Test_update_task() {
    var name = "Centazio Unit Test Task (do not delete): " + Guid.NewGuid();
    await (await GetApi()).UpdateTask(TEST_TASK_ID, name); 
  }
  
  [Test] public async Task Test_close_task() { 
    await (await GetApi()).CloseTask(TEST_TASK_ID); 
  }
  
  [Test] public async Task Test_open_task() { 
    await (await GetApi()).OpenTask(TEST_TASK_ID); 
  }
  
  [Test, Ignore("Do not delete, as there is no way to get task back")] public async Task Test_delete_task() { 
    await (await GetApi()).DeleteTask(TEST_TASK_ID); 
  }
  
  private async Task<ClickUpApi> GetApi() => new(await F.Settings<Settings>(), await F.Secrets<Secrets>());
}
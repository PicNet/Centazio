using Centazio.Core.Misc;
using Centazio.Sample.ClickUp;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpApiTests {

  // https://app.clickup.com/t/{test_task_id}
  private readonly string test_task_id = "86cxvdxet";
  
  [Test] public async Task Test_print_task_list() {
    var tasks = await Api.GetTasksAfter(UtcDate.UtcNow.AddYears(-10));
    DevelDebug.WriteLine("TASKS:\n\t" + String.Join("\n\t", tasks.Select(t => t.Json)));
  }
  
  [Test] public async Task Test_create_task() {
    var start = DateTime.UtcNow;
    // using DateTime.UtcNow on purpose as we want empty result set
    var first = await Api.GetTasksAfter(DateTime.UtcNow);
    var name = $"{nameof(ClickUpApiTests)}:{Guid.NewGuid()}";
    var id = await Api.CreateTask(name);
    
    var taskjson = (await Api.GetTasksAfter(start)).Single();
    var task = Json.Deserialize<ClickUpTask>(taskjson.Json);
    await Api.DeleteTask(id);
    
    Assert.That(first, Is.Empty);
    Assert.That(task.id, Is.EqualTo(id));
    Assert.That(task.name, Is.EqualTo(name));
    Assert.That(await Api.GetTasksAfter(start), Is.Empty);
  }
  
  [Test] public async Task Test_update_task() {
    var name = "Centazio Unit Test Task (do not delete): " + Guid.NewGuid();
    await Api.UpdateTask(test_task_id, name); 
  }
  
  [Test] public async Task Test_close_task() { 
    await Api.CloseTask(test_task_id); 
  }
  
  [Test] public async Task Test_open_task() { 
    await Api.OpenTask(test_task_id); 
  }
  
  [Test, Ignore("Do not delete, as there is no way to get task back")] public async Task Test_delete_task() { 
    await Api.DeleteTask(test_task_id); 
  }
  
  private ClickUpApi Api => new(F.Settings<SampleSettings>().ClickUp, F.Secrets<SampleSecrets>());

}
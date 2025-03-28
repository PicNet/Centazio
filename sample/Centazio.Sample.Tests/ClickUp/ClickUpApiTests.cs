﻿using Centazio.Core.Misc;
using Centazio.Sample.ClickUp;
using Centazio.Sample.Shared;
using Centazio.Test.Lib;

namespace Centazio.Sample.Tests.ClickUp;

public class ClickUpApiTests {

  // https://app.clickup.com/t/{test_task_id}
  internal static readonly string TEST_TASK_ID = "86cxvdxet";
  
  [Test] public async Task Test_print_task_list() {
    var tasks = await Api.GetTasksAfter(UtcDate.UtcNow.AddYears(-10));
    DevelDebug.WriteLine("TASKS:\n\t" + String.Join("\n\t", tasks.Select(t => t.Json)));
  }
  
  [Test] public async Task Test_create_task() {
    var start = DateTime.UtcNow; // using DateTime.UtcNow on purpose as we want empty result set
    var first = await Api.GetTasksAfter(start);
    var name = $"{nameof(ClickUpApiTests)}:{Guid.NewGuid()}";
    if (Env.IsGitHubActions()) await Task.Delay(250); // delays to avoid flakyness in DI
    var id = await Api.CreateTask(name);
    if (Env.IsGitHubActions()) await Task.Delay(250);
    var tasks = await Api.GetTasksAfter(start.AddMinutes(-1));
    Console.WriteLine($"start[{start}] Is GH[{Env.IsGitHubActions()}] id[{id}] tasjs[{tasks.Count}]");
    var task = tasks
        .Select(t => Json.Deserialize<ClickUpTask>(t.Json))
        .Single(t => t.id == id);
    await Api.DeleteTask(id);
    
    Assert.That(first, Is.Empty);
    Assert.That(task.id, Is.EqualTo(id));
    Assert.That(task.name, Is.EqualTo(name));
    Assert.That(await Api.GetTasksAfter(start), Is.Empty);
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
  
  private ClickUpApi Api => new(F.Settings<Settings>(), F.Secrets<Secrets>());

}
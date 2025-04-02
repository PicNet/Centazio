using Centazio.Core.Misc;
using Centazio.Sample.AppSheet;
using Centazio.Sample.ClickUp;
using Centazio.Sample.Shared;
using Centazio.Sample.Tests.ClickUp;

namespace Centazio.Sample.Tests;

public class ResetSampleDbsAndApis {
  
  [Test] public async Task Reset() {
    ResetDb();
    await ResetApis();
  }

  private void ResetDb() => File.Delete(FsUtils.GetDevPath("sample.db"));

  private async Task ResetApis() {
    var (settings, secrets) = (F.Settings<Settings>(), F.Secrets<Secrets>());
    var (clickup, appsheet) =  (new ClickUpApi(settings, secrets), new AppSheetApi(settings.AppSheet, secrets));
    await DeleteClickUpTasks(clickup);
    await DeleteAppSheetTasks(appsheet);
  }

  private static async Task DeleteClickUpTasks(ClickUpApi clickup) {
    var tasks = (await clickup.GetTasksAfter(UtcDate.UtcNow.AddYears(-10)))
        .Select(json => Json.Deserialize<ClickUpTask>(json.Json))
        .Where(t => t.id != ClickUpApiTests.TEST_TASK_ID)
        .ToList();
    await tasks.Select(t => clickup.DeleteTask(t.id)).Synchronous();
  }

  private async Task DeleteAppSheetTasks(AppSheetApi appsheet) {
    var tasks = (await appsheet.GetAllTasks()).Select(Json.Deserialize<AppSheetTask>).ToList();
    await appsheet.DeleteTasks(tasks);
  }

}
using Centazio.Core.Misc;
using Centazio.Sample.AppSheet;
using Centazio.Sample.ClickUp;

namespace Centazio.Sample.Tests;

[Ignore("only run manually")] 
public class ResetSampleDbsAndApis {
  
  [Test] public async Task Reset() {
    ResetDb();
    await ResetApis();
  }

  private void ResetDb() => File.Delete(Path.Combine(FsUtils.GetSolutionRootDirectory(), "sample.db"));

  private async Task ResetApis() {
    var (settings, secrets) = (F.Settings<SampleSettings>(), F.Secrets<SampleSecrets>());
    var (clickup, appsheet) =  (new ClickUpApi(settings.ClickUp, secrets), new AppSheetApi(settings.AppSheet, secrets));
    await DeleteClickUpTasks(clickup);
    await DeleteAppSheetTasks(appsheet);
  }

  private static async Task DeleteClickUpTasks(ClickUpApi clickup) {
    var tasks = (await clickup.GetTasksAfter(UtcDate.UtcNow.AddYears(-10))).Select(json => Json.Deserialize<ClickUpTask>(json.Json)).ToList();
    await tasks.Select(t => clickup.DeleteTask(t.id)).Synchronous();
  }

  private async Task DeleteAppSheetTasks(AppSheetApi appsheet) {
    var tasks = (await appsheet.GetAllTasks()).Select(Json.Deserialize<AppSheetTask>).ToList();
    await appsheet.DeleteTasks(tasks);
  }

}
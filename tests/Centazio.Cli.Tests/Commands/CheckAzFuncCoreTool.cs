using Centazio.Cli.Infra.Misc;

namespace Centazio.Cli.Tests.Commands;

public class CheckAzFuncCoreTool {

  private static readonly ICommandRunner cmd = new CommandRunner();
  
  [Test] public async Task Test_AzureCoreTools_IsLatestVersion() {
    var result = await cmd.Run("func", "--version", Directory.GetCurrentDirectory());
    Assert.That(String.IsNullOrWhiteSpace(result.Err), "Error checking Azure Functions Core Tools version");

    var match = System.Text.RegularExpressions.Regex.Match(result.Out, @"(\d+\.\d+\.\d+)");
    Assert.That(match.Success, "Failed to parse current version number");

    var ver = match.Groups[1].Value;

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("User-Agent", "C-Test");
    var resp = await http.GetStringAsync("https://registry.npmjs.org/azure-functions-core-tools/latest");

    var doc = System.Text.Json.JsonDocument.Parse(resp);
    var latest = doc.RootElement.GetProperty("version").GetString();

    Assert.That(latest is not null, "Failed to retrieve latest version");

    var current = new Version(ver);
    if (latest != null) {
     var newest = new Version(latest);

     Assert.That(current, Is.GreaterThanOrEqualTo(newest),$"Azure Functions Core Tools is not the latest version. Current: {ver}, Latest: {latest}");
    }
  }
}
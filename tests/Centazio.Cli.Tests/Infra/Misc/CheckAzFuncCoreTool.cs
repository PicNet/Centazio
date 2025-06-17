using System.Text.Json;
using Centazio.Cli.Infra.Misc;

namespace Centazio.Cli.Tests.Infra.Misc;

public class CheckAzFuncCoreTool {

  private static readonly ICommandRunner cmd = new CommandRunner();
  
  [Test] public async Task Test_AzureCoreTools_IsLatestVersion() {
    var currver = await cmd.Run("func", "--version", Directory.GetCurrentDirectory());
    var match = System.Text.RegularExpressions.Regex.Match(currver.Out, @"(\d+\.\d+\.\d+)");
    var latest = await GetLatestVerFromNpmRegister();
    var (currverval, latestval) = (new Version(match.Groups[1].Value), new Version(latest));
    
    Assert.That(String.IsNullOrWhiteSpace(currver.Err), "Error checking Azure Functions Core Tools version");
    Assert.That(match.Success, "Failed to parse current version number");
    Assert.That(currverval, Is.GreaterThanOrEqualTo(latestval),$"Azure Functions Core Tools is not the latest version. Current: {currverval}, Latest: {latest}");

    async Task<string> GetLatestVerFromNpmRegister() {
      using var http = new HttpClient();
      http.DefaultRequestHeaders.Add("User-Agent", "C-Test");
      var resp = await http.GetStringAsync("https://registry.npmjs.org/azure-functions-core-tools/latest");

      return JsonDocument.Parse(resp).RootElement.GetProperty("version").GetString() 
          ?? throw new Exception("Failed to retrieve latest version");
    }
  }
}
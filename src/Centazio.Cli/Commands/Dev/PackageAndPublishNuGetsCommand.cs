using System.Text.RegularExpressions;
using Centazio.Core.Secrets;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Dev;

public class PackageAndPublishNuGetsCommand(CentazioSecrets secrets, ICommandRunner runner) : AbstractCentazioCommand<PackageAndPublishNuGetsCommand.Settings> {

  private readonly string packagesdir = "packages";
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings());
  
  public override Task ExecuteImpl(Settings settings) {
    if (!Env.IsInDev()) throw new Exception(nameof(PackageAndPublishNuGetsCommand) + " should not be accessible outside of the Centazio dev environment");
    var cwd = FsUtils.GetCentazioPath();
    // package
    FsUtils.EmptyDirectory(Path.Combine(cwd, packagesdir));
    if (!settings.NoBump) BumpVersionBuildNumber();
    runner.DotNet($"pack -c Release -o {packagesdir}", cwd);
    
    // publish 
    if (settings.NoPublish) return Task.CompletedTask; 
    var apikey = secrets.NUGET_API_KEY ?? throw new ArgumentNullException(nameof(secrets.NUGET_API_KEY));
    runner.DotNet($"nuget push ./{packagesdir}/*.nupkg --source https://api.nuget.org/v3/index.json --api-key {apikey}", cwd);
    
    return Task.CompletedTask;
  }

  private void BumpVersionBuildNumber() {
    var path = FsUtils.GetCentazioPath("Directory.Build.props");
    var content = File.ReadAllText(path);
    var pattern = @"<Version>(\d+)\.(\d+)\.(\d+)((-[a-zA-Z0-9]+)?)</Version>";

    content = Regex.Replace(content, pattern, match => {
      var major = match.Groups[1].Value;
      var minor = match.Groups[2].Value;
      var patch = Int32.Parse(match.Groups[3].Value);
      var suffix = match.Groups[4].Value;
      
      return $"<Version>{major}.{minor}.{patch + 1}{suffix}</Version>";
    });
    File.WriteAllText(path, content);

  }

  public class Settings : CommonSettings {
    [CommandOption("-b|--no-bump")] public bool NoBump { get; init; }
    [CommandOption("-p|--no-publish")] public bool NoPublish { get; init; }
  }
}
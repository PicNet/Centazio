using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Runner;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Az;

public class AzFunctionLocalSimulateCommand(
    [FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSettings coresettings, 
    ICliSecretsManager secrets, 
    ICommandRunner cmd, 
    ITemplater templater) : AbstractCentazioCommand<AzFunctionLocalSimulateCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings { 
    AssemblyNames = UiHelpers.Ask("Assembly Name (csv, pattern)"),
    FunctionNames = UiHelpers.Ask("Function Names (csv)"),
  });

  public override async Task ExecuteImpl(Settings settings) {
    var projects = ReflectionUtils.LoadAssembliesFuzzy(settings.AssemblyNames, [coresettings.Defaults.GeneratedCodeFolder])
        .Where(ass => IntegrationsAssemblyInspector.GetCentazioFunctions(ass, []).Any())
        .Select(ass => new AzFunctionProjectMeta(ass, coresettings, templater)).ToList();
    
    if (!projects.Any()) throw new CentazioCommandNiceException($"The <ASSEMBLY_NAMES> pattern(s) did not match any valid function assemblies. Please check pattern used '{settings.AssemblyNames}'");
    
    // no need to await, as it will just run in the background
    _ = cmd.Run("azurite", coresettings.Defaults.ConsoleCommands.Az.RunAzuriteArgs, quiet: true);
    
    if (!settings.NoGenerate) {
      await projects.Select(project => 
          UiHelpers.Progress($"Generating Azure Function project '{project.DashedProjectName}'", async () => await new AzCloudSolutionGenerator(coresettings, secrets, templater, project, settings.EnvironmentsList).GenerateSolution()))
          .Synchronous();
    }
    if (!settings.NoBuild) {
      await projects.Select(project => 
          UiHelpers.Progress("Building and publishing project", async () => await new DotNetCliProjectPublisher(coresettings, templater).PublishProject(project)))
          .Synchronous();
    }
    
    var result = await cmd.Aws($"configure export-credentials --format process --profile {coresettings.AwsSettings.AccountName}");
    var cred = Json.Deserialize<AwsCredentails>(result.Out);
    
    await Task.WhenAll(projects.Select(project => {
      var functions = settings.FunctionNames is null ? null : String.Join(" ", settings.FunctionNames.Split(','));

      Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", cred.AccessKeyId);
      Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", cred.SecretAccessKey);
      Environment.SetEnvironmentVariable("AWS_SESSION_TOKEN", cred.SessionToken);
      Environment.SetEnvironmentVariable("AWS_CREDENTIAL_EXPIRATION", cred.Expiration);
      
      var funcstart = templater.ParseFromContent(coresettings.Defaults.ConsoleCommands.Func.LocalSimulateFunction, new { Functions = functions });
      return cmd.Func(funcstart, cwd: project.PublishPath, input: "1");
    }));
  }
  
  public class Settings : CommonSettings {
    [CommandArgument(0, "<ASSEMBLY_NAMES>")]
    [Description("This argument supports comma separated assembly names, or wildcards (*)")]
    public required string AssemblyNames { get; init; }
    
    [CommandArgument(1, "[FUNCTION_NAMES]")]
    [Description("This argument supports comma separated function names")]
    public string? FunctionNames { get; init; }
    
    [CommandOption("-g|--no-generate")] [DefaultValue(false)] public bool NoGenerate { get; set; }
    [CommandOption("-b|--no-build")] [DefaultValue(false)] public bool NoBuild { get; set; }
  }
  
  public record AwsCredentails  {
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string SessionToken { get; set; }
    public required string Expiration { get; set; }
  }
}

using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;

namespace Centazio.Cli.Tests.Infra.Misc; 

public class CommandRunnerTests {
  
  private static readonly string VERSION = "--version";
  
  private readonly ICommandRunner cmd = new CommandRunner();
  
  [Test] public async Task Test_az() {
    var results = await cmd.Az(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("az"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetCentazioPath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
  }
  
  [Test] public async Task Test_func() {
    var results = await cmd.Func(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("func"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetCentazioPath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
  }
  
  [Test] public async Task Test_dotnet() {
    var results = await cmd.DotNet(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("dotnet"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetCentazioPath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
  }
  
  [Test] public async Task Test_aws() {
    var results = await cmd.Aws(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("aws"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetCentazioPath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
  }

  [Test] public void Test_Redact_hides_credentials() {
    Assert.That(CommandRunner.Redact("nuget push packages\\*.nupkg --source https://api.nuget.org/v3/index.json --api-key abc123"),
        Is.EqualTo("nuget push packages\\*.nupkg --source https://api.nuget.org/v3/index.json --api-key ***"));
    Assert.That(CommandRunner.Redact("push --API-KEY=abc123 --verbose"), Is.EqualTo("push --API-KEY=*** --verbose"));
    Assert.That(CommandRunner.Redact("login --password \"a b c\" --user x"), Is.EqualTo("login --password *** --user x"));
    Assert.That(CommandRunner.Redact(VERSION), Is.EqualTo(VERSION));
  }

  [Test] public void Test_ToString_redacts_args() {
    var results = new CommandResults(null, "dotnet", "nuget push x.nupkg --api-key abc123", ".", String.Empty, String.Empty);
    Assert.That(results.ToString(), Is.EqualTo("dotnet nuget push x.nupkg --api-key ***"));
  }
}
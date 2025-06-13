using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;

namespace Centazio.Cli.Tests.Infra.Dotnet; 

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
  
  [Test] public async Task Test_docker() {
    var results = await cmd.Docker(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("docker"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetCentazioPath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
  }
}
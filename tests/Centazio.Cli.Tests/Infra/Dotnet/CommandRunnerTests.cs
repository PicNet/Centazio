using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Misc;

namespace Centazio.Cli.Tests.Infra.Dotnet; 

public class CommandRunnerTests {
  
  private static readonly string VERSION = "--version";
  
  private readonly CommandRunner cmd = new ();
  
  [Test] public void Test_az() {
    var results = cmd.Az(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("az"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetSolutionFilePath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
    Assert.That(!results.NewWindow);
  }
  
  [Test] public void Test_func() {
    var results = cmd.Func(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("func"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetSolutionFilePath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
    Assert.That(!results.NewWindow);
  }
  
  [Test] public void Test_dotnet() {
    var results = cmd.DotNet(VERSION);
    
    Assert.That(results.Args, Is.EqualTo(VERSION));
    Assert.That(results.Command, Is.EqualTo("dotnet"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetSolutionFilePath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
    Assert.That(!results.NewWindow);
  }
  
  [Test] public void Test_mstest() {
    var results = cmd.MsTest(VERSION);
    
    Assert.That(results.Args, Is.EqualTo("test " + VERSION));
    Assert.That(results.Command, Is.EqualTo("dotnet"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetSolutionFilePath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
    Assert.That(!results.NewWindow);
  }
  
  [Test] public void Test_mstest_in_new_window() {
    var results = cmd.MsTest(VERSION, newwindow: true);
    
    Assert.That(results.Args, Is.EqualTo("test " + VERSION));
    Assert.That(results.Command, Is.EqualTo("dotnet"));
    Assert.That(results.Dir, Is.EqualTo(FsUtils.GetSolutionFilePath()));
    Assert.That(results.Err, Is.Not.Null);
    Assert.That(results.Out, Is.Not.Null);
    Assert.That(results.NewWindow);
  }
}
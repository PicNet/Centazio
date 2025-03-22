using Centazio.Cli.Commands.Gen.Centazio;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Misc;

namespace Centazio.Cli.Tests.Commands;

public class GenerateSlnCommandTests {

  [Test] public async Task Go() {
    Templater.TestingRootDir = FsUtils.GetSolutionRootDirectory();
    Environment.CurrentDirectory = @"c:\dev";
    
    var sln = nameof(GenerateSlnCommandTests);
    var cmd = new GenerateSlnCommand(new CommandRunner());
    if (Directory.Exists(sln)) Directory.Delete(sln, true);
    await cmd.ExecuteImpl(new GenerateSlnCommand.Settings { SolutionName = sln });
  }

}
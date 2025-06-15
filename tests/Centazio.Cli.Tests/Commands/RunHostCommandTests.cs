using Centazio.Cli.Commands.Host;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Hosts.Self;

namespace Centazio.Cli.Tests.Commands;

public class RunHostCommandTests {

  [Test] public async Task Test_command_runs_for_2_seconds_without_errors() {
    var cmd = new RunHostCommand(await F.Settings(), new SelfHost(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token));
    await cmd.ExecuteImpl(new RunHostCommand.Settings { AssemblyNames = "Centazio.TestFunctions", Environments = [CentazioConstants.DEFAULT_ENVIRONMENT] });
  }
  
  [Test] public async Task Test_command_runs_for_2_seconds_without_errors_AppSheet() {
    // todo WT: fails with 'No service for type 'Centazio.Sample.Shared.CoreStorageRepository' has been registered.' which
    //    is due to 'Aws' secrets loader.  If you add `SecretsFolder` even with 'Aws' provider, then this test passes.
    if (Env.IsGitHubActions) return;
    
    var cmd = new RunHostCommand(await F.Settings(), new SelfHost(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token));
    await cmd.ExecuteImpl(new RunHostCommand.Settings { AssemblyNames = "Centazio.Sample.AppSheet", Environments = [CentazioConstants.DEFAULT_ENVIRONMENT] });
  }
}

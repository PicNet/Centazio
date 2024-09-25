using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Systems;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests;

public class Environment : IAsyncDisposable {
  
  // Infra
  private readonly IStagedEntityStore sestore = new InMemoryStagedEntityStore(0, s => s.GetHashCode().ToString());
  private readonly ICtlRepository ctl = new InMemoryCtlRepository();
  private readonly List<ISystem> Systems;
  
  // Crm
  private readonly CrmSystem crm = new();
  private readonly CrmReadFunction crm_read;
  private FunctionRunner<ReadOperationConfig, ReadOperationResult> crm_read_runner;
  private ReadOperationRunner read_op_runner;
  
  // Fin
  private readonly FinSystem fin = new();
  
  public Environment() {
    Systems = [crm, fin];
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(
        crm_read = new CrmReadFunction(crm), 
        read_op_runner = new ReadOperationRunner(sestore), 
        ctl);
  }
  
  public async ValueTask DisposeAsync() {
    await sestore.DisposeAsync();
    await ctl.DisposeAsync();
  }

  private const int TOTAL_EPOCHS = 500;
  
  [Test] public void RunSimulation() {
    Enumerable.Range(0, TOTAL_EPOCHS).ForEach(epoch => {
      if (epoch % 25 == 0) Helpers.DebugWrite($"running simulation epoch[{epoch} / {TOTAL_EPOCHS}]");
      TestingUtcDate.DoTick(new TimeSpan(1, Random.Shared.Next(0, 24), Random.Shared.Next(0, 60), Random.Shared.Next(0, 60)));
      Systems.ForEach(s => s.Step());
    });
  }
}
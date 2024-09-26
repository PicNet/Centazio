using Centazio.Core.Ctl;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Extensions;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests;

public class E2EEnvironment : IAsyncDisposable {

  private const int TOTAL_EPOCHS = 500;
  private readonly CoreStorage core = new();

  // Crm
  private readonly CrmSystem crm = new();
  private readonly CrmPromoteFunction crm_promote;
  private readonly CrmReadFunction crm_read;
  private readonly CrmWriteFunction crm_write;
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> crm_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> crm_promote_runner;
  // todo: function runner should allow operations of SingleWrite and Batch write combined.  Currently new
  //    functions are required if we need to combine these
  private readonly FunctionRunner<BatchWriteOperationConfig, WriteOperationResult> crm_write_runner;

  // Fin
  private readonly FinSystem fin = new();

  // Infra
  private readonly ICtlRepository ctl = new InMemoryCtlRepository();
  private readonly IEntityIntraSystemMappingStore entitymap = new InMemoryEntityIntraSystemMappingStore();
  private readonly IStagedEntityStore sestore = new InMemoryStagedEntityStore(0, s => s.GetHashCode().ToString());
  private readonly List<ISystem> Systems;
  

  public E2EEnvironment() {
    Systems = [crm, fin];
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(crm_read = new CrmReadFunction(crm),
        new ReadOperationRunner(sestore),
        ctl);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(crm_promote = new CrmPromoteFunction(core),
        new PromoteOperationRunner(sestore, entitymap, core),
        ctl);
    
    crm_write_runner = new FunctionRunner<BatchWriteOperationConfig, WriteOperationResult>(crm_write = new CrmWriteFunction(crm),
        new WriteOperationRunner<CoreCustomer, BatchWriteOperationConfig>(entitymap, core), 
        ctl);
  }

  public async ValueTask DisposeAsync() {
    await sestore.DisposeAsync();
    await ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() {
    await Enumerable.Range(0, TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
    ValidateSimulation();
  }

  private async Task<int> RunEpoch(int epoch) {
    if (epoch % 25 == 0) Helpers.DebugWrite($"running simulation epoch[{epoch} / {TOTAL_EPOCHS}]");

    TestingUtcDate.DoTick(new TimeSpan(1, Random.Shared.Next(0, 24), Random.Shared.Next(0, 60), Random.Shared.Next(0, 60)));

    Systems.ForEach(s => s.Step());

    List<Task> functions = [
      crm_read_runner.RunFunction(),
      crm_promote_runner.RunFunction()
    ];
    await Shuffle(functions).Synchronous();
    
    ValidateEpoch();

    return epoch;
  }

  private IList<T> Shuffle<T>(IList<T> list) {
    var n = list.Count;
    while (n > 1) {
      n--;
      var k = Random.Shared.Next(n + 1);
      (list[k], list[n]) = (list[n], list[k]);
    }
    return list;
  }
  
  private void ValidateEpoch() { throw new NotImplementedException(); }
  
  private void ValidateSimulation() { throw new NotImplementedException(); }
}
using System.Text.Json;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Tests.Read;
using Centazio.Test.Lib;
// using H = Centazio.Test.Lib.Helpers;

namespace Centazio.Core.Tests.DummySystems.Crm;

public class DummyCrmReadFunction(
        ICtlRepository ctl, 
        FunctionConfig<ReadOperationConfig> cfg, 
        IOperationRunner<ReadOperationConfig> runner) 
    : AbstractReadFunction(ctl, cfg, runner) {

  // todo: make this mandatory in the IFunction interface
  public FunctionConfig<ReadOperationConfig> Config { get; } = cfg;

  public static DummyCrmReadFunction Create(ICtlRepository ctl, IStagedEntityStore stager) {
    var (sys, stg, obj) = (Constants.CrmSystemName, Constants.Read, Constants.CrmCustomer);
    var api = new DummyCrmApiConsumer2();
    var alldt = UtcDate.UtcNow.AddYears(-1);
    var cfg = new FunctionConfig<ReadOperationConfig>(sys, Constants.Read, new List<ReadOperationConfig> {
      new(obj, TestingDefaults.CRON_EVERY_SECOND, alldt, async config => {
        Assert.That((await ctl.GetSystemState(sys, stg))!.Status, Is.EqualTo(ESystemStateStatus.Running));
        
        var after = config.Checkpoint;
        var customers = await api.GetCustomersUpdatedSince(after);
        
        // H.DebugWrite($"function run now[{H.SecsDiff()}] start[{H.SecsDiff(start)}] after[{H.SecsDiff(after)}] customer[{customers.Count}]");
        
        return customers.Any() ? 
                new ListRecordOperationResult(EOperationResult.Success, $"{customers.Count}", customers)
                : new EmptyOperationResult(EOperationResult.Success, $"{customers.Count}");
      })
    }); 
    var oprunner = TestingFactories.ReadRunner(stager);
    return new DummyCrmReadFunction(ctl, cfg, oprunner);
  }

  private class DummyCrmApiConsumer2 {

    // this list mantains a list of customers in the dummy database.   A new customer is added
    //    each second (utc.NowNoIncrement - TEST_START_DT) with the LastUpdate date being utc.NowNoIncrement 
    private readonly List<CrmCustomer> customers = new();

    public Task<List<string>> GetCustomersUpdatedSince(DateTime after) {
      UpdateCustomerList();
      return Task.FromResult(customers.Where(c => c.LastUpdate > after).Select(c => JsonSerializer.Serialize(c)).ToList());
    }

    private void UpdateCustomerList() {
      var now = UtcDate.UtcNow;
      var expcount = (int)(now - TestingDefaults.DefaultStartDt).TotalSeconds;
      var start = customers.Count;
      var missing = expcount - start;
      Enumerable.Range(0, missing)
          .ForEachIdx(missidx => {
            var idx = start + missidx;
            customers.Add(NewCust(idx));
          });
    }
  }

  internal static CrmCustomer NewCust(int idx, DateTime? updated = null) => new(
      Guid.Parse($"00000000-0000-0000-0000-{idx.ToString().PadLeft(12, '0')}"), 
      idx.ToString(), 
      idx.ToString(), 
      DateOnly.FromDateTime(TestingDefaults.DefaultStartDt.AddYears(-idx)), 
      updated ?? UtcDate.UtcNow);
}

public class DummyCrmReadFunctionTests {
  
  [Test] public async Task E2e_test_of_standalone_read_function() {
    // set up
    var (start, ctl, stager) = (UtcDate.UtcNow, TestingFactories.CtlRepo(), TestingFactories.SeStore());
    var func = DummyCrmReadFunction.Create(ctl, stager);
    // todo: config should be a method in IFunction and should then not need to be passed to FunctionRunner ctor
    var funcrunner = new FunctionRunner<ReadOperationConfig>(func, func.Config, ctl);

    async Task<IEnumerable<OperationResult>> RunFunc(bool isfirst = false) {
      var startstate = await ctl.GetSystemState(func.Config.System, func.Config.Stage);
      if (isfirst) Assert.That(startstate, Is.Null);
      else Assert.That(startstate!.Status, Is.EqualTo(ESystemStateStatus.Idle));
      var results = await funcrunner.RunFunction();
      Assert.That((await ctl.GetSystemState(func.Config.System, func.Config.Stage))!.Status, Is.EqualTo(ESystemStateStatus.Idle));
      return results.OpResults;
    }
    
    // run scenarios
    var (sys0, obj0) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged0 = await stager.Get(UtcDate.UtcNow.AddYears(-1), Constants.CrmSystemName, Constants.CrmCustomer);
    
    // this run should be empty as no TestingUtcDate.DoTick
    var r1 = (EmptyOperationResult) (await RunFunc(true)).Single();
    var (sys1, obj1) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged1 = await stager.Get(UtcDate.UtcNow.AddYears(-1), Constants.CrmSystemName, Constants.CrmCustomer);
    
    // this should include the single customer added as a List result type
    var onetick = TestingUtcDate.DoTick();
    var r2 = (ListRecordOperationResult) (await RunFunc()).Single();
    var (sys2, obj2) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged2 = await stager.Get(UtcDate.UtcNow.AddYears(-1), Constants.CrmSystemName, Constants.CrmCustomer);
    
    var r3 = await RunFunc(); // should be empty as no time has passed and Cron expects max 1/sec
    var (sys3, obj3) = (ctl.Systems.Values.ToList(), ctl.Objects.Values.ToList());
    var staged3 = await stager.Get(UtcDate.UtcNow.AddYears(-1), Constants.CrmSystemName, Constants.CrmCustomer);
    
    // validate results
    var expjson = JsonSerializer.Serialize(DummyCrmReadFunction.NewCust(0, onetick));
    Assert.That(r1, Is.EqualTo(new EmptyOperationResult(EOperationResult.Success, "0")));
    Assert.That(r2, Is.EqualTo(new ListRecordOperationResult(EOperationResult.Success, "1", r2.PayloadList)));
    Assert.That(r2.PayloadList.Value.Single(), Is.EqualTo(expjson));
    Assert.That(r3, Is.Empty);
    
    // validate sys/obj states and staged entities
    Assert.That(sys0, Is.Empty);
    Assert.That(obj0, Is.Empty);
    Assert.That(staged0, Is.Empty);
    
    Assert.That(sys1.Single(), Is.EqualTo(new SystemState(Constants.CrmSystemName, Constants.Read, true, start, ESystemStateStatus.Idle, start, start, start)));
    Assert.That(obj1.Single(), Is.EqualTo(new ObjectState(Constants.CrmSystemName, Constants.Read, Constants.CrmCustomer, true, start, EOperationResult.Success, EOperationAbortVote.Continue, start, start, start, start, start, "operation [Crm/Read/CrmCustomer] completed [Success] message: 0", 0)));
    Assert.That(staged1, Is.Empty);
    
    Assert.That(sys2.Single(), Is.EqualTo(new SystemState(Constants.CrmSystemName, Constants.Read, true, start, ESystemStateStatus.Idle, onetick, onetick, onetick)));
    Assert.That(obj2.Single(), Is.EqualTo(new ObjectState(Constants.CrmSystemName, Constants.Read, Constants.CrmCustomer, true, start, EOperationResult.Success, EOperationAbortVote.Continue, onetick, onetick, onetick, onetick, onetick, "operation [Crm/Read/CrmCustomer] completed [Success] message: 1", 1) { LastPayLoadType = EResultType.List }));
    Assert.That(staged2.Single(), Is.EqualTo(new StagedEntity(Constants.CrmSystemName, Constants.CrmCustomer, onetick, expjson, TestingFactories.TestingChecksum(expjson))));
    
    Assert.That(sys3.Single(), Is.EqualTo(new SystemState(Constants.CrmSystemName, Constants.Read, true, start, ESystemStateStatus.Idle, onetick, onetick, onetick)));
    Assert.That(obj3.Single(), Is.EqualTo(new ObjectState(Constants.CrmSystemName, Constants.Read, Constants.CrmCustomer, true, start, EOperationResult.Success, EOperationAbortVote.Continue, onetick, onetick, onetick, onetick, onetick, "operation [Crm/Read/CrmCustomer] completed [Success] message: 1", 1) { LastPayLoadType = EResultType.List }));
    Assert.That(staged3.Single(), Is.EqualTo(new StagedEntity(Constants.CrmSystemName, Constants.CrmCustomer, onetick, expjson, TestingFactories.TestingChecksum(expjson))));
  }
}


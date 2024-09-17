using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests.Write;

public class WriteFunctionTests {
  [Test] public Task Test() {
    Assert.Fail("implement");
    return Task.CompletedTask;
  }

}

public class WriteFunctionWithSingleWriteCustomerOperation : AbstractWriteFunction<CoreCustomer> {

  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  public WriteFunctionWithSingleWriteCustomerOperation() {
    Config = new(Constants.CrmSystemName, Constants.Write, new ([
      new BatchWriteOperationConfig<CoreCustomer>(Constants.CrmCustomer, TestingDefaults.CRON_EVERY_SECOND, UtcDate.UtcNow.AddYears(-1), WriteEntitiesToTargetSystem)
    ]));
  }

  private Task<WriteOperationResult<CoreCustomer>> WriteEntitiesToTargetSystem(BatchWriteOperationConfig<CoreCustomer> config, List<(CoreCustomer Core, EntityIntraSystemMapping Map)> entities) { throw new NotImplementedException(); }

}
using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpPromoteFunction(IStagedEntityRepository stager, ICoreStorage core, ICtlRepository ctl) : PromoteFunction(Constants.CLICK_UP, stager, core, ctl) {

  private readonly string EVERY_X_SECONDS_NCRON = "*/5 * * * * *";
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(ClickUpTask), Constants.CU_TASK, Constants.TASK, EVERY_X_SECONDS_NCRON, PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    throw new NotImplementedException();
  }

}

public record ClickUpTask;
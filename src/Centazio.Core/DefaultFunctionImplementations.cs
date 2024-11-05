using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;

namespace Centazio.Core.Read;

public abstract class ReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {
  public abstract Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config);
}

public class ReadFunctionRunner(IEntityStager stager, ICtlRepository ctl) : FunctionRunner<ReadOperationConfig, ReadOperationResult>(new ReadOperationRunner(stager), ctl);

public abstract class PromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  public abstract Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval);
}

public class PromoteFunctionRunner(IStagedEntityRepository stage, ICoreStorage core, ICtlRepository ctl) : FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new PromoteOperationRunner(stage, core, ctl), ctl);

public abstract class WriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {
  public abstract Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreEntitiesToSystemEntities(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
  public abstract Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate);
}

public class WriteFunctionRunner(ICoreStorage core, ICtlRepository ctl) : FunctionRunner<WriteOperationConfig, WriteOperationResult>(new WriteOperationRunner<WriteOperationConfig>(ctl, core), ctl);
using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public interface IGetObjectsToStage {
  Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config);
}

public record ReadOperationConfig(ExternalEntityType ExternalEntityType, ValidCron Cron, IGetObjectsToStage GetObjectsToStage) 
        : OperationConfig<ExternalEntityType>(ExternalEntityType, Cron) {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public ExternalEntityType ExternalEntityType { get; init; } = ExternalEntityType;
}
        
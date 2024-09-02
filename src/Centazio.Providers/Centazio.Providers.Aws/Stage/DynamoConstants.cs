using Centazio.Core.Ctl.Entities;

namespace Centazio.Providers.Aws.Stage;

public class DynamoConstants {

  public static readonly string HASH_KEY = $"{nameof(StagedEntity.SourceSystem)}|{nameof(StagedEntity.Object)}";
  public static readonly string RANGE_KEY = $"{nameof(StagedEntity.DateStaged)}|Guid";

}
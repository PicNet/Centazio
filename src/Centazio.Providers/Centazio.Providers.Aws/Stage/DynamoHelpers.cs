using Centazio.Core;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Providers.Aws.Stage;

public class DynamoHelpers {

  public static readonly string HASH_KEY = $"{nameof(StagedEntity.SourceSystem)}|{nameof(StagedEntity.Object)}";
  public static readonly string RANGE_KEY = $"{nameof(StagedEntity.DateStaged)}|Guid";
  
  public static string ToHashKey(SystemName system, ObjectName obj) => $"{system.Value}|{obj.Value}";
  public static string ToRangeKey(DateTime staged, Guid suffix) => $"{staged:o}|{suffix}";

}
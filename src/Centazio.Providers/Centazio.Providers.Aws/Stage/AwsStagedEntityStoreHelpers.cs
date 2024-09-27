using Centazio.Core;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Providers.Aws.Stage;

public class AwsStagedEntityStoreHelpers {

  public static readonly string DYNAMO_HASH_KEY = $"{nameof(StagedEntity.SourceSystem)}|{nameof(StagedEntity.Object)}";
  public static readonly string DYNAMO_RANGE_KEY = $"{nameof(StagedEntity.DateStaged)}|Guid";
  
  public static string ToDynamoHashKey(SystemName system, ObjectName obj) => $"{system.Value}|{obj.Value}";
  public static string ToDynamoRangeKey(DateTime staged, Guid suffix) => $"{staged:o}|{suffix}";
  
  public static string ToS3Key(StagedEntity e) => $"{e.SourceSystem.Value}/{e.Object.Value}/{e.DateStaged:o}_{e.Checksum}_{e.Id}";
  public static S3KeyComponents ParseS3Key(string key) {
    var (system, entity, rest, _) = key.Split('/');
    var (stagedstr, checksum, idstr, _) = rest.Split('_');
    return new S3KeyComponents(system, new(entity), DateTime.Parse(stagedstr).ToUniversalTime(), checksum, Guid.Parse(idstr));
  } 
}

public record S3KeyComponents(SystemName System, ExternalEntityType Object, DateTime DateStaged, ValidString Checksum, Guid Id);
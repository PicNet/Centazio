using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Stage;

namespace Centazio.Providers.Aws.Stage;

public class AwsStagedEntityRepositoryHelpers {

  public static readonly string DYNAMO_HASH_KEY = $"{nameof(StagedEntity.System)}|{nameof(StagedEntity.SystemEntityTypeName)}";
  public static readonly string DYNAMO_RANGE_KEY = $"{nameof(StagedEntity.DateStaged)}|Guid";
  
  public static string ToDynamoHashKey(SystemName system, SystemEntityTypeName systype) => $"{system.Value}|{systype.Value}";
  public static string ToDynamoRangeKey(DateTime staged, Guid suffix) => $"{staged:o}|{suffix}";
  
  public static string ToS3Key(StagedEntity e) => $"{e.System.Value}/{e.SystemEntityTypeName.Value}/{e.DateStaged:o}_{e.StagedEntityChecksum}_{e.Id}_{e.CorrelationId}";
  public static S3KeyComponents ParseS3Key(string key) {
    var (system, entity, rest, _) = key.Split('/');
    // corridparts is required as we cannot add corrid, as `Split` at most returns 4 out params
    var (stagedstr, checksum, idstr, corridparts) = rest.Split('_');
    var corrid = new CorrelationId(String.Join('_', corridparts));
    return new S3KeyComponents(new(system), new(entity), DateTime.Parse(stagedstr).ToUniversalTime(), new(checksum), Guid.Parse(idstr), corrid);
  } 
}

public record S3KeyComponents(SystemName System, SystemEntityTypeName SystemEntityTypeName, DateTime DateStaged, StagedEntityChecksum StagedEntityChecksum, Guid Id, CorrelationId CorrelationId);
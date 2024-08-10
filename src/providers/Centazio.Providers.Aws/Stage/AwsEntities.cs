using Centazio.Core;
using Centazio.Core.Entities.Ctl;

namespace Centazio.Providers.Aws.Stage;

public record AwsStagedEntity(string RangeKey, SystemName SourceSystem, ObjectName EntityName, DateTime DateStaged, string Data, DateTime? DatePromoted = null, string? Ignore = null);

public static class AwsEntityExtensionMethods {
  public static AwsStagedEntity ToAwsStagedEntity(this StagedEntity se) => new AwsStagedEntity($"{se.DateStaged.Ticks}|{Guid.NewGuid()}", se.SourceSystem, se.Object, se.DateStaged, se.Data, se.DatePromoted, se.Ignore);
}
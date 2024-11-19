namespace Centazio.Core.Misc;

[AttributeUsage(AttributeTargets.All)] public class IgnoreNamingConventionsAttribute : Attribute;
[AttributeUsage(AttributeTargets.All)] public class MaxLength2Attribute(int length) : Attribute {
  public int Length { get; } = length;
}
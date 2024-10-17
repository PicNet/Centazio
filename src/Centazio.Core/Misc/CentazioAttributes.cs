namespace Centazio.Core.Misc;

[AttributeUsage(AttributeTargets.All)] public class IgnoreNamingConventionsAttribute : Attribute;
[AttributeUsage(AttributeTargets.All)] public class MaxLength2Attribute(int Length) : Attribute {
  public int Length { get; } = Length;
}
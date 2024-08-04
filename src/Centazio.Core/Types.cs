namespace Centazio.Core;

public interface IStringValue { 
  string Value { get; init; }
  
  public string ToString() => Value;
}

public record SystemName(string Value) : IStringValue;
public record ObjectName(string Value) : IStringValue;
public record LifecycleStage(string Value) : IStringValue;
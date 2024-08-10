namespace Centazio.Core;

public interface IStringValue { 
  string Value { get; init; }
  
  public string ToString() => Value;
}

public record SystemName(string Value) : IStringValue {
  public static implicit operator SystemName(string value) => new(value);
};
public record ObjectName(string Value) : IStringValue {
  public static implicit operator ObjectName(string value) => new(value);
}
public record LifecycleStage(string Value) : IStringValue {
  public static implicit operator LifecycleStage(string value) => new(value);
}

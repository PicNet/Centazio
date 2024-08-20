namespace Centazio.Core;

public interface IStringValue { 
  string Value { get; init; }
}

public record ValidString(string Value) : IStringValue {
  public string Value { get; init; } = !String.IsNullOrWhiteSpace(Value) 
      ? Value.Trim() : throw new ArgumentException("Value must be a non-empty string", nameof(Value));
  
  public static implicit operator string(ValidString value) => value.Value;
  public static explicit operator ValidString(string value) => new (value);
}

public record SystemName(string Value) : IStringValue {
  public SystemName() : this("") {}
  public static implicit operator SystemName(string value) => new(value);
}

public record ObjectName(string Value) : IStringValue {
  public ObjectName() : this("") {}
  public static implicit operator ObjectName(string value) => new(value);
}

public record LifecycleStage(string Value) : IStringValue {
  public LifecycleStage() : this("") {}
  public static implicit operator LifecycleStage(string value) => new(value);
}

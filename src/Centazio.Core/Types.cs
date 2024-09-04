namespace Centazio.Core;

public record ValidList<T>(IReadOnlyList<T> Value) {
  public IReadOnlyList<T> Value { get; } = !Value.Any() 
      ? throw new ArgumentException("Value must be anon-empty list", nameof(Value)) 
      : Value.Any(o => EqualityComparer<T>.Default.Equals(o, default)) 
          ? throw new ArgumentException("Value must no contain any null elements", nameof(Value))
          : Value;
}

public record ValidString(string Value) {
  public string Value { get; } = !String.IsNullOrWhiteSpace(Value) 
      ? Value.Trim() : throw new ArgumentException("Value must be a non-empty string", nameof(Value));
  
  public static implicit operator string(ValidString value) => value.Value;
  public static implicit operator ValidString(string value) => new(value);
  
  public sealed override string ToString() => Value;
}

public sealed record SystemName(string Value) : ValidString(Value) {
  public static implicit operator SystemName(string value) => new(value);
}

public sealed record ObjectName(string Value) : ValidString(Value) {
  public static implicit operator ObjectName(string value) => new((ValidString) value);
}

public sealed record LifecycleStage(string Value) : ValidString(Value) {
  public static implicit operator LifecycleStage(string value) => new((ValidString) value);
}

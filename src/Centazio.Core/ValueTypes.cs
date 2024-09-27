using Centazio.Core.CoreRepo;

namespace Centazio.Core;

public record ValidList<T>(IReadOnlyList<T> Value) {
  public IReadOnlyList<T> Value { get; } = !Value.Any() 
      ? throw new ArgumentException("Value must be anon-empty list", nameof(Value)) 
      : Value.Any(o => EqualityComparer<T>.Default.Equals(o, default)) 
          ? throw new ArgumentException("Value must no contain any null elements", nameof(Value))
          : Value;
  
  public static implicit operator List<T>(ValidList<T> value) => value.Value.ToList();
  public static implicit operator ValidList<T>(List<T> value) => new(value.AsReadOnly());
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

public sealed record CoreEntityName {
  public string Name { get;}
  
  private CoreEntityName(ValidString name) { Name = name; }
  
  public static CoreEntityName From<T>() where T : ICoreEntity => new(typeof(T).Name);
}

public sealed record LifecycleStage(string Value) : ValidString(Value) {
  public static implicit operator LifecycleStage(string value) => new((ValidString) value);
  
  public static class Defaults {
    public static readonly LifecycleStage Read = new(nameof(Read));
    public static readonly LifecycleStage Promote = new(nameof(Promote));
    public static readonly LifecycleStage Write = new(nameof(Write));
  }
}

﻿using Centazio.Core.CoreRepo;

namespace Centazio.Core;

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

public record ObjectName : ValidString {
  internal ObjectName(string Value) : base(Value) {}
}

public sealed record ExternalEntityType(string Value) : ObjectName(Value);

public sealed record CoreEntityType(string Value) : ObjectName(Value) {
  public static CoreEntityType From<E>() where E : ICoreEntity => new(typeof(E).Name);
}

public sealed record LifecycleStage(string Value) : ValidString(Value) {
  public static implicit operator LifecycleStage(string value) => new((ValidString) value);
  
  public static class Defaults {
    public static readonly LifecycleStage Read = new(nameof(Read));
    public static readonly LifecycleStage Promote = new(nameof(Promote));
    public static readonly LifecycleStage Write = new(nameof(Write));
  }
}

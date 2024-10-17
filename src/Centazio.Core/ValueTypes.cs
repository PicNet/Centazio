﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;

namespace Centazio.Core;

public record ValidString(string Value) {
  public string Value { get; } = !String.IsNullOrWhiteSpace(Value) 
      ? Value.Trim() : throw new ArgumentException("Value must be a non-empty string", nameof(Value));
  
  public static implicit operator string(ValidString value) => value.Value;
  public static implicit operator ValidString(string value) => new(value);
  
  public sealed override string ToString() => Value;
  
  public static List<Type> AllSubclasses() {
    return typeof(ValidString).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ValidString))).ToList();
  }
}

public abstract record EntityId(string Value) : ValidString(Value) { public const int MAX_LENGTH = 64; }
public sealed record CoreEntityId(string Value) : EntityId(Value);
public sealed record SystemEntityId(string Value) : EntityId(Value);

[MaxLength2(32)] public sealed record SystemName(string Value) : ValidString(Value) {
  public const int MAX_LENGTH = 32; // todo: remove
  public static implicit operator SystemName(string value) => new(value);
}

[MaxLength2(32)] public record ObjectName : ValidString {
  public const int MAX_LENGTH = 32; // todo: remove
  internal ObjectName(string Value) : base(Value) {}
  
  internal SystemEntityTypeName ToSystemEntityTypeName => this as SystemEntityTypeName ?? throw new Exception($"expected [{this}] to be of type '{nameof(ToSystemEntityTypeName)}'");
  internal CoreEntityTypeName ToCoreEntityTypeName => this as CoreEntityTypeName ?? throw new Exception($"expected [{this}] to be of type '{nameof(CoreEntityTypeName)}'");
}

public sealed record SystemEntityTypeName(string Value) : ObjectName(Value) {
  public static SystemEntityTypeName From<E>() where E : ISystemEntity => new(typeof(E).Name);
  public static SystemEntityTypeName From<E>(E sysent) where E : ISystemEntity => new(sysent.GetType().Name);
}

public sealed record CoreEntityTypeName(string Value) : ObjectName(Value) {
  public static CoreEntityTypeName From<E>() where E : ICoreEntity => new(typeof(E).Name);
  public static CoreEntityTypeName From<E>(E core) where E : ICoreEntity => new(core.GetType().Name);
}

[MaxLength2(32)] public sealed record LifecycleStage(string Value) : ValidString(Value) {
  public static implicit operator LifecycleStage(string value) => new((ValidString) value);
  
  [IgnoreNamingConventions] 
  public static class Defaults {
    public static readonly LifecycleStage Read = new(nameof(Read));
    public static readonly LifecycleStage Promote = new(nameof(Promote));
    public static readonly LifecycleStage Write = new(nameof(Write));
  }
}

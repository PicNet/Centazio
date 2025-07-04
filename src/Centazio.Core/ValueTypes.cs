using Cronos;

namespace Centazio.Core;

public record ValidString(string Value) {
  public string Value { get; } = !String.IsNullOrWhiteSpace(Value) 
      ? Value.Trim() : throw new ArgumentException("Value must be a non-empty string", nameof(Value));
  
  public static implicit operator string(ValidString value) => value.Value;
  public static explicit operator ValidString(string value) => new(value);
  
  public sealed override string ToString() => Value;
  
  public static List<Type> AllSubclasses() {
    return typeof(ValidString).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(ValidString))).ToList();
  }
}

[MaxLength2(128)] public record CorrelationId(string Value) : ValidString(Value) {
  public static readonly CorrelationId DEFAULT_VALUE = new("0");
  
  public static CorrelationId Build(SystemName system, SystemEntityId id) => new($"{system.Value}[{id.Value}]");
}

[MaxLength2(64)] public abstract record EntityId(string Value) : ValidString(Value);

public record CoreEntityId(string Value) : EntityId(Value) {
  public static readonly CoreEntityId DEFAULT_VALUE = new("0");
}

public record SystemEntityId(string Value) : EntityId(Value) {
  public static readonly SystemEntityId DEFAULT_VALUE = new("*");
}

[MaxLength2(32)] public record NotSystem(SystemName System) : SystemName(System.Value);

[MaxLength2(32)] public record SystemName(string Value) : ValidString(Value);

[MaxLength2(32)] public record ObjectName(string Value) : ValidString(Value) {

  internal SystemEntityTypeName ToSystemEntityTypeName => this as SystemEntityTypeName ?? throw new Exception($"expected [{this}] to be of type '{nameof(ToSystemEntityTypeName)}'");
  internal CoreEntityTypeName ToCoreEntityTypeName => this as CoreEntityTypeName ?? throw new Exception($"expected [{this}] to be of type '{nameof(CoreEntityTypeName)}'");
}

public record SystemEntityTypeName(string Value) : ObjectName(Value) {
  public static SystemEntityTypeName From<E>() where E : ISystemEntity => new(typeof(E).Name);
  public static SystemEntityTypeName From<E>(E sysent) where E : ISystemEntity => new(sysent.GetType().Name);
}

public record CoreEntityTypeName(string Value) : ObjectName(Value) {
  public static CoreEntityTypeName From<E>() where E : ICoreEntity => new(typeof(E).Name);
  public static CoreEntityTypeName From<E>(E core) where E : ICoreEntity => new(core.GetType().Name);
  public static CoreEntityTypeName From(Type coretype) => new(coretype.Name);
}

[MaxLength2(32)] public record LifecycleStage(string Value) : ValidString(Value) {
  [IgnoreNamingConventions] 
  public static class Defaults {
    public static readonly LifecycleStage Read = new(nameof(Read));
    public static readonly LifecycleStage Promote = new(nameof(Promote));
    public static readonly LifecycleStage Write = new(nameof(Write));
  }
}

public record ValidCron {
  public ValidCron(string expression) {
    ArgumentException.ThrowIfNullOrWhiteSpace(expression);
    Value = CronExpression.Parse(Expression = expression.Trim(), CronFormat.IncludeSeconds); 
  }
  
  public string Expression { get; }
  public CronExpression Value { get; }
}
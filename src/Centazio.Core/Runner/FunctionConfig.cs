using Centazio.Core.Checksum;

namespace Centazio.Core.Runner;

public record FunctionConfigDefaults {
  public static bool ThrowExceptions { get; set; }
  public static int TimeoutMinutes { get; set; } = 15;
  public static DateTime DefaultFirstTimeCheckpoint { get; set; } = UtcDate.UtcNow.AddMonths(-1);
}

public interface IFunctionConfig : IDisposable {
  int TimeoutMinutes { get; init; }

  /// <summary>
  /// When a function operation is run for the first time, there is no 'last run'.  This is used as a
  /// replacement for this 'last run' value and any data since this date is considered ready for processing.
  /// Note: This can be overwritten in each operation's specific `OperationConfig`.
  /// </summary>
  DateTime DefaultFirstTimeCheckpoint { get; init; }

  /// <summary>
  /// Whether to log and swallow exceptions, or throw instead.  Logging exceptions works well
  /// for serverless environments where exceptions add no value, but on local hosted or testing
  /// environments Exceptions add more value.
  /// </summary>
  bool ThrowExceptions { get; init; }

  IChecksumAlgorithm ChecksumAlgorithm { get; init; }
}

public record BaseFunctionConfig : IFunctionConfig {
  public int TimeoutMinutes { get; init; } = FunctionConfigDefaults.TimeoutMinutes;

  public DateTime DefaultFirstTimeCheckpoint { get; init; } = FunctionConfigDefaults.DefaultFirstTimeCheckpoint;

  public bool ThrowExceptions { get; init; } = FunctionConfigDefaults.ThrowExceptions;

  private IChecksumAlgorithm? checksum;
  public IChecksumAlgorithm ChecksumAlgorithm {
    get => checksum ??= new Sha256ChecksumAlgorithm();
    init {
      if (checksum is not null) { checksum.Dispose(); }
      checksum = value;
    }
  }
  
  public void Dispose() { checksum?.Dispose(); }
}

// todo: FunctionConfig should not take <O> generic argument as Functions handle multiple operations for multiple objects
public record FunctionConfig<C, O>(
    SystemName System, 
    LifecycleStage Stage, 
    List<C> Operations) : BaseFunctionConfig, ILoggable 
        where C : OperationConfig<O>
        where O : ObjectName {

  public List<C> Operations { get; } = Operations.Any() ? Operations : throw new ArgumentNullException(nameof(Operations));
  
  public object LoggableValue => $"{System}/{Stage} Operations[{Operations.Count}]";
}
using Centazio.Core.Checksum;

namespace Centazio.Core.Runner;

public record FunctionConfig(List<OperationConfig> Operations) : IDisposable {
  public int TimeoutMinutes { get; init; }
  
  /// <summary>
  /// When a function operation is run for the first time, there is no 'last run'.  This is used as a
  /// replacement for this 'last run' value and any data since this date is considered ready for processing.
  /// Note: This can be overwritten in each operation's specific `OperationConfig`.  When neither is set, one month
  /// before the first run (by the function's clock) is used.
  /// </summary>
  public DateTime? DefaultFirstTimeCheckpoint { get; init; }
  
  /// <summary>
  /// Whether to log and swallow exceptions, or throw instead.  Logging exceptions works well
  /// for serverless environments where exceptions add no value, but on local hosted or testing
  /// environments Exceptions add more value.  When not set, the host's default (given to the `FunctionRunner`) is used.
  /// </summary>
  public bool? ThrowExceptions { get; init; }
  
  /// <summary>
  /// Allows the setting of the host polling frequency (usnig Cron/NCron syntax).  If this is not set then we use the
  /// settings.[cloud].json properties: ReadFunctionPollExpression, PromoteFunctionPollExpression, WriteFunctionPollExpression, OtherFunctionPollExpression
  /// </summary>
  public string? FunctionPollExpression { get; init; }

  public List<OperationConfig> Operations { get; } = Operations.Any() ? Operations : throw new ArgumentNullException(nameof(Operations));
  
  private IChecksumAlgorithm? checksum;
  public IChecksumAlgorithm ChecksumAlgorithm {
    get => checksum ??= new Sha256ChecksumAlgorithm();
    init {
      if (checksum is not null) { checksum.Dispose(); }
      checksum = value;
    }
  }
  
  public void Dispose() { checksum?.Dispose(); }
  
  public string LoggableValue => $"Operations[{Operations.Count}]";
}

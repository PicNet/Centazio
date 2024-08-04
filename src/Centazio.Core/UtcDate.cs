namespace Centazio.Core;

public interface IUtcDate {

  DateTime Now { get; }
  DateTime Today { get; }

}

public abstract class AbstractUtcDate : IUtcDate {

  public abstract DateTime Now { get; }
  public DateTime Today => Now.Date;

}

public class UtcDate(Func<DateTime>? now = null) : AbstractUtcDate {
  private readonly Func<DateTime> now = now ?? (() => DateTime.UtcNow);
  
  public override DateTime Now => now();
  
}

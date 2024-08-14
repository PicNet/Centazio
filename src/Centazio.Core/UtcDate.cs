namespace Centazio.Core;

public interface IUtcDate {

  DateTime Now { get; }
  DateTime Today { get; }

}

public abstract class AbstractUtcDate : IUtcDate {

  public abstract DateTime Now { get; }
  public DateTime Today => Now.Date;

}

public class UtcDate : AbstractUtcDate {
  public override DateTime Now => DateTime.UtcNow;
}

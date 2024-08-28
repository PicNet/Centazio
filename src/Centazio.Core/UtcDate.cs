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

  private static IUtcDate? instance;
  public static IUtcDate Utc { get => instance ?? throw new Exception($"UtcDate.Utc has not been initialised"); set => instance = value; }
  public override DateTime Now => DateTime.UtcNow;
}

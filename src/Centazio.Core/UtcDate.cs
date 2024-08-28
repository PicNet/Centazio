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
  
  // Static Helpers:
  private static IUtcDate? instance;
  public static IUtcDate Utc { get => instance ?? throw new Exception($"UtcDate.Utc has not been initialised"); set => instance = value ?? throw new ArgumentNullException(nameof(Utc)); }
  public static DateTime UtcNow => Utc.Now;
}

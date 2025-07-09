using System.Text.RegularExpressions;

namespace Centazio.Core.Misc;

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
  
  private static IUtcDate instance = new RealUtcDate();
  internal static IUtcDate Utc { get => instance; set => instance = value ?? throw new ArgumentNullException(nameof(Utc)); }
  
  public static DateTime UtcNow => Utc.Now;
  public static DateTime UtcToday => Utc.Today;
  
  public static DateTime FromMillis(string source, string? regex = null) => regex is null ? FromMillis(Int64.Parse(source)) : FromMillis(Regex.Match(source, regex).Groups[1].Value);
  public static DateTime FromMillis(long millis) => DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeMilliseconds(millis).DateTime, DateTimeKind.Utc);
  public static long ToMillis(DateTime? dt = null) => (long) (dt ?? UtcNow).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
}


public class RealUtcDate : AbstractUtcDate { public override DateTime Now => DateTime.UtcNow; }
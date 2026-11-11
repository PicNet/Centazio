using System.Text.RegularExpressions;

namespace Centazio.Core.Misc;

/// <summary>
/// Pure date conversions.  The current time is never read statically, it comes from the `TimeProvider` injected into
/// the engine, functions and repositories (see `GlobalMiscExtensionMethods.UtcNow`).
/// </summary>
public static class UtcDate {
  public static DateTime FromMillis(string millis, string? regex = null) => regex is null ? FromMillis(Int64.Parse(millis)) : FromMillis(Regex.Match(millis, regex).Groups[1].Value);
  public static DateTime FromMillis(long millis) => DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeMilliseconds(millis).DateTime, DateTimeKind.Utc);
  public static long ToMillis(DateTime dt) => (long) dt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
}
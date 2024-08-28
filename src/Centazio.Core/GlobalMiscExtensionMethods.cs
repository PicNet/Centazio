namespace Centazio.Core;

public static class GlobalMiscExtensionMethods {

  public static string IfNullOrWhitespace(this string str, string other) {
    return String.IsNullOrWhiteSpace(str) ? other.Trim() : str.Trim();
  }
}
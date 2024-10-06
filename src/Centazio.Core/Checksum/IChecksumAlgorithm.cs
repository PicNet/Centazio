namespace Centazio.Core.Checksum;

/// <summary>
/// A checksum used to check for unnecessary writes to core storage or the target system.
/// If this returns null then checksum comparisons will not be made and all updates
/// written even if nothing meaningful has changed.
///
/// Implementing methods should return a subset of the entity fields that
/// signify meaningful changes.
///
/// This means ignoring redundant Ids, children collections, date updated, etc.
///
/// Example:
/// ```
/// public object? GetChecksumSubset() => new { Name, Address };
/// ```
/// </summary>
public interface IGetChecksumSubset {
  object? GetChecksumSubset();
}

public interface IChecksumAlgorithm : IDisposable {
  string Checksum(IGetChecksumSubset obj);
}

public interface IStringChecksumAlgorithm : IDisposable {
  string Checksum(string str);
} 
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;

namespace Centazio.Core.Checksum;

public interface IChecksumAlgorithm : IDisposable {
  SystemEntityChecksum Checksum(ISystemEntity sysent);
  CoreEntityChecksum Checksum(ICoreEntity coreent);
  StagedEntityChecksum Checksum(string str);
}

[MaxLength2(MAX_LENGTH)] public abstract record ChecksumValue(string Value) : ValidString(Value) {
  public const int MAX_LENGTH = 64;
}
public record StagedEntityChecksum(string Value) : ChecksumValue(Value);
public record SystemEntityChecksum(string Value) : ChecksumValue(Value);
public record CoreEntityChecksum(string Value) : ChecksumValue(Value);
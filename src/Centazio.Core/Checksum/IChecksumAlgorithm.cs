using Centazio.Core.CoreRepo;

namespace Centazio.Core.Checksum;

public interface IChecksumAlgorithm : IDisposable {
  SystemEntityChecksum Checksum(ISystemEntity sysent);
  CoreEntityChecksum Checksum(ICoreEntity coreent);
  StagedEntityChecksum Checksum(string str);
}

public abstract record ChecksumValue(string Value) : ValidString(Value);
public record StagedEntityChecksum(string Value) : ChecksumValue(Value);
public record SystemEntityChecksum(string Value) : ChecksumValue(Value);
public record CoreEntityChecksum(string Value) : ChecksumValue(Value);
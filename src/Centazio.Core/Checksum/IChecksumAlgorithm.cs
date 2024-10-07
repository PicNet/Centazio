using Centazio.Core.CoreRepo;

namespace Centazio.Core.Checksum;

public interface IChecksumAlgorithm : IDisposable {
  SystemEntityChecksum Checksum(ISystemEntity e);
  CoreEntityChecksum Checksum(ICoreEntity e);
  StagedEntityChecksum Checksum(string str);
}

public abstract record ChecksumValue(string Value) : ValidString(Value);
public record StagedEntityChecksum(string Value) : ChecksumValue(Value);
public record SystemEntityChecksum(string Value) : ChecksumValue(Value);
public record CoreEntityChecksum(string Value) : ChecksumValue(Value);
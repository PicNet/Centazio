using System.Text;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Core.Checksum;

public interface IChecksumAlgorithm : IDisposable {
  SystemEntityChecksum Checksum(ISystemEntity sysent);
  CoreEntityChecksum Checksum(ICoreEntity coreent);
  StagedEntityChecksum Checksum(string data);
}

public abstract class AbstractChecksumAlgorith : IChecksumAlgorithm {

  public SystemEntityChecksum Checksum(ISystemEntity sysent) {
    var subset = sysent.GetChecksumSubset();
    if (sysent.SystemId == SystemEntityId.DEFAULT_VALUE) throw new ArgumentException($"Checksum should not be calculated on a SystemEntity if its SystemId has not been set");
    
    return new(GetChecksumImpl(GetSubsetBytes(subset)));
  }
  
  public CoreEntityChecksum Checksum(ICoreEntity coreent) {
    var subset = coreent.GetChecksumSubset();
    if (coreent.CoreId == CoreEntityId.DEFAULT_VALUE) throw new ArgumentException($"Checksum should not be calculated on a CoreEntity if its CoreId has not been set");
    
    return new(GetChecksumImpl(GetSubsetBytes(subset)));
  }

  public StagedEntityChecksum Checksum(string data) => new(GetChecksumImpl(GetStrBytes(data)));
  
  private byte[] GetSubsetBytes(object subset) => GetStrBytes(Json.Serialize(subset));
  private byte[] GetStrBytes(string data) => Encoding.UTF8.GetBytes(data);
  
  public abstract void Dispose();
  protected abstract string GetChecksumImpl(byte[] bytes);
}

[MaxLength2(MAX_LENGTH)] public abstract record ChecksumValue(string Value) : ValidString(Value) {
  public const int MAX_LENGTH = 64;
}
public record StagedEntityChecksum(string Value) : ChecksumValue(Value);
public record SystemEntityChecksum(string Value) : ChecksumValue(Value);
public record CoreEntityChecksum(string Value) : ChecksumValue(Value);
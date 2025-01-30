using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Test.Lib;

public static class Helpers {
  
  public static readonly IChecksumAlgorithm TestingChecksumAlgorithm = new ChecksumAlgo();
  
  public static StagedEntityChecksum TestingStagedEntityChecksum(string data) => TestingChecksumAlgorithm.Checksum(data);
  public static CoreEntityChecksum TestingCoreEntityChecksum(ICoreEntity coreent) => TestingChecksumAlgorithm.Checksum(coreent);
  public static SystemEntityChecksum TestingSystemEntityChecksum(ISystemEntity sysent) => TestingChecksumAlgorithm.Checksum(sysent);
  
  public class ChecksumAlgo : IChecksumAlgorithm {
    
    public SystemEntityChecksum Checksum(ISystemEntity sysent) => new(Json.Serialize(sysent.GetChecksumSubset()).GetHashCode().ToString());
    public CoreEntityChecksum Checksum(ICoreEntity coreent) => new(Json.Serialize(coreent.GetChecksumSubset()).GetHashCode().ToString());
    public StagedEntityChecksum Checksum(string str) => new (str.GetHashCode().ToString());

    public void Dispose() {}
  }
}
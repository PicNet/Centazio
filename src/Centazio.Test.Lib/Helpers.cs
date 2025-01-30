using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Types;

namespace Centazio.Test.Lib;

public static class Helpers {
  
  public static StagedEntityChecksum TestingStagedEntityChecksum(string data) => new (data.GetHashCode().ToString());
  public static CoreEntityChecksum TestingCoreEntityChecksum(ICoreEntity obj) => new (ChecksumImpl(obj));
  public static SystemEntityChecksum TestingSystemEntityChecksum(ISystemEntity obj) => new(ChecksumImpl(obj));
  public static IChecksumAlgorithm TestingChecksumAlgorithm = new ChecksumAlgo();
  
  private static string ChecksumImpl(IGetChecksumSubset obj) => Json.Serialize(obj.GetChecksumSubset()).GetHashCode().ToString();
  
  public class ChecksumAlgo : IChecksumAlgorithm {
    
    public SystemEntityChecksum Checksum(ISystemEntity sysent) => TestingSystemEntityChecksum(sysent);
    public CoreEntityChecksum Checksum(ICoreEntity coreent) => TestingCoreEntityChecksum(coreent);
    public StagedEntityChecksum Checksum(string str) => TestingStagedEntityChecksum(str);

    public void Dispose() {}
  }
}
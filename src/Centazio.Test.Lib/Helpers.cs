using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib;

public static class Helpers {
  
  public static string SecsDiff(DateTime? dt = null) => ((int) ((dt ?? UtcDate.UtcNow) - TestingDefaults.DefaultStartDt).TotalSeconds).ToString();

  public static StagedEntityChecksum TestingStagedEntityChecksum(string data) => new (data.GetHashCode().ToString());
  public static CoreEntityChecksum TestingCoreEntityChecksum(ICoreEntity obj) => new (ChecksumImpl(obj));
  public static SystemEntityChecksum TestingSystemEntityChecksum(ISystemEntity obj) => new(ChecksumImpl(obj));
  private static string ChecksumImpl(IGetChecksumSubset obj) => Json.Serialize(obj.GetChecksumSubset()).GetHashCode().ToString();
  
  public class ChecksumAlgo : IChecksumAlgorithm {
    
    public SystemEntityChecksum Checksum(ISystemEntity sysent) => TestingSystemEntityChecksum(sysent);
    public CoreEntityChecksum Checksum(ICoreEntity coreent) => TestingCoreEntityChecksum(coreent);
    public StagedEntityChecksum Checksum(string str) => TestingStagedEntityChecksum(str);

    public void Dispose() {}
  }
}
using Centazio.Core;
using Centazio.Core.Checksum;

namespace Centazio.Test.Lib;

public static class Helpers {
  
  public static readonly IChecksumAlgorithm TestingChecksumAlgorithm = new TestingHashcodeBasedChecksumAlgo();
  
  public static StagedEntityChecksum TestingStagedEntityChecksum(string data) => TestingChecksumAlgorithm.Checksum(data);
  public static CoreEntityChecksum TestingCoreEntityChecksum(ICoreEntity coreent) => TestingChecksumAlgorithm.Checksum(coreent);
  public static SystemEntityChecksum TestingSystemEntityChecksum(ISystemEntity sysent) => TestingChecksumAlgorithm.Checksum(sysent);
  
  public class TestingHashcodeBasedChecksumAlgo : AbstractChecksumAlgorith {
    public override void Dispose() {}
    
    protected override string GetChecksumImpl(byte[] bytes) => String.Join('.',bytes).GetHashCode().ToString();
  }
}
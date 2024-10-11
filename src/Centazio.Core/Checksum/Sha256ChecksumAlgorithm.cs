using System.Security.Cryptography;
using System.Text;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Checksum;

public class Sha256ChecksumAlgorithm : IChecksumAlgorithm {

  private readonly SHA256 sha = SHA256.Create();
  
  public SystemEntityChecksum Checksum(ISystemEntity e) => new(Impl(Json.Serialize(e.GetChecksumSubset())));
  public CoreEntityChecksum Checksum(ICoreEntity e) => new(Impl(Json.Serialize(e.GetChecksumSubset())));
  public StagedEntityChecksum Checksum(string str) => new(Impl(str));

  private string Impl(string str) {
    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(str.Trim()));
    return Convert.ToHexString(hash);
  }

  public void Dispose() => sha.Dispose();

}
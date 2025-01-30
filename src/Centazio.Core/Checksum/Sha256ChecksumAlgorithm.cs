using System.Security.Cryptography;

namespace Centazio.Core.Checksum;

public class Sha256ChecksumAlgorithm : AbstractChecksumAlgorith {

  private readonly SHA256 sha = SHA256.Create();
  
  public override void Dispose() => sha.Dispose();
  
  protected override string GetChecksumImpl(byte[] bytes) => Convert.ToHexString(sha.ComputeHash(bytes));
}
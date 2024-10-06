using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Centazio.Core.Checksum;

public class Sha256ChecksumAlgorithm : IChecksumAlgorithm, IStringChecksumAlgorithm {

  private readonly SHA256 sha = SHA256.Create();
  
  public string Checksum(IGetChecksumSubset obj) => Checksum(JsonSerializer.Serialize(obj.GetChecksumSubset()));

  public string Checksum(string str) {
    if (String.IsNullOrWhiteSpace(str)) return String.Empty;
    var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
    return BitConverter.ToString(checksum).Replace("-", String.Empty);
  }

  public void Dispose() => sha.Dispose();

}
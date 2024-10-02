using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Centazio.Core.Checksum;

public class Sha256ChecksumAlgorithm : IChecksumAlgorithm {

  private readonly SHA256 sha = SHA256.Create();
  
  public string Checksum(object obj) {
    var str = JsonSerializer.Serialize(obj);
    var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
    return BitConverter.ToString(checksum).Replace("-", String.Empty);
  }

  public void Dispose() => sha.Dispose();

}
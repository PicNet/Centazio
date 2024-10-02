using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Centazio.Core.Checksum;

public class DefaultChecksumAlgorithm {

  // todo: not calling dispose may not be possible to have as static
  private static readonly SHA256 sha = SHA256.Create();
  
  public static string Checksum(object obj) {
    var str = JsonSerializer.Serialize(obj);
    var checksum = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
    return BitConverter.ToString(checksum).Replace("-", String.Empty);
  }

}
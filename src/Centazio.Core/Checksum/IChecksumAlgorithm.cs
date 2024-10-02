namespace Centazio.Core.Checksum;

public interface IChecksumAlgorithm : IDisposable {
  public string Checksum(object obj);
}
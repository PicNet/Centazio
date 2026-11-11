namespace Centazio.Core.Misc;

public interface IGuidGenerator {
  Guid NewGuid();
}

public class Version7GuidGenerator : IGuidGenerator {
  public Guid NewGuid() => Guid.CreateVersion7();
}

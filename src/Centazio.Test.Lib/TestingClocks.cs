using Microsoft.Extensions.Time.Testing;

namespace Centazio.Test.Lib;

public static class TestingClockExtensions {
  public static DateTime Tick(this FakeTimeProvider clock, TimeSpan? interval = null) {
    clock.Advance(interval ?? TimeSpan.FromSeconds(1));
    return clock.UtcNow();
  }
}

/// <summary>
/// Returns the scripted instants one per `GetUtcNow` call, then defers to the fallback clock.  Used where a test needs
/// time to move backwards, which `FakeTimeProvider` does not allow.
/// </summary>
public class ScriptedTimeProvider(TimeProvider fallback) : TimeProvider {
  private readonly Queue<DateTime> script = [];
  
  public void Script(List<DateTime> instants) => instants.ForEach(script.Enqueue);
  
  public override DateTimeOffset GetUtcNow() => script.TryDequeue(out var next) ? new DateTimeOffset(next) : fallback.GetUtcNow();
}

public class SeededGuidGenerator(Random rng) : IGuidGenerator {
  public Guid NewGuid() {
    var bytes = new byte[16];
    rng.NextBytes(bytes);
    return new Guid(bytes);
  }
}

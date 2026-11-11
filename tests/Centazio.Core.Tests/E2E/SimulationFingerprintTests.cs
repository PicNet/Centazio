using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Test.Lib.E2E;

namespace Centazio.Core.Tests.E2E;

public class SimulationFingerprintTests {

  private static readonly Guid ID1 = new("00000000-0000-0000-0000-000000000001");
  private static readonly Guid ID2 = new("00000000-0000-0000-0000-000000000002");
  
  [Test] public void Test_ignored_entity_id_and_reason_are_part_of_the_fingerprint() {
    var original = Fingerprint(ID1, "reason");
    
    Assert.That(Fingerprint(ID1, "reason"), Is.EqualTo(original));
    Assert.That(Fingerprint(ID2, "reason"), Is.Not.EqualTo(original));
    Assert.That(Fingerprint(ID1, "other reason"), Is.Not.EqualTo(original));
  }
  
  [Test] public void Test_fingerprint_chains_from_the_previous_epoch() {
    var state = new { Value = 1 };
    Assert.That(SimulationFingerprint.Next("a", state), Is.Not.EqualTo(SimulationFingerprint.Next("b", state)));
  }

  private string Fingerprint(Guid ignoredid, string reason) {
    var staged = new StagedEntity(ignoredid, C.System1Name, C.SystemEntityName, TestingDefaults.DefaultStartDt, new("{}"), new("checksum"));
    var result = new SuccessPromoteOperationResult([], [(staged, new(reason))]);
    var results = new SuccessFunctionRunResults([new OpResultAndObject(C.CoreEntityName, result)]);
    return SimulationFingerprint.Next(String.Empty, new List<RunSnapshot> { SimulationFingerprint.Snapshot(GetType(), results) });
  }
}

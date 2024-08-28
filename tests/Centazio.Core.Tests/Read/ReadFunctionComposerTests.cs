using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Test.Lib;

namespace centazio.core.tests.Read;

public class ReadFunctionComposerTests {
  
  private readonly ReadFunctionConfig cfg = new(nameof(ReadFunctionComposerTests), nameof(ReadFunctionComposerTests), []);

  [Test] public async Task Test_RunOperationsTillAbort_on_single_valid_op() {
    var now = new TestingUtcDate();
    var repo = ReadTestFactories.Repo(now);
    var runner = ReadTestFactories.Runner(now, repo: repo);
    var composer = ReadTestFactories.Composer(now, cfg, runner, repo);
    
    var states1 = new List<ReadOperationStateAndConfig> { await CreateReadOpStateAndConf(repo, EOperationReadResult.Success) };
    var results1 = await composer.RunOperationsTillAbort(now.Now, states1);
    
    var states2 = new List<ReadOperationStateAndConfig> { await CreateReadOpStateAndConf(repo, EOperationReadResult.Warning) };
    var results2 = await composer.RunOperationsTillAbort(now.Now, states2);
    
    var states3 = new List<ReadOperationStateAndConfig> { await CreateReadOpStateAndConf(repo, EOperationReadResult.FailedRead) };
    var results3 = await composer.RunOperationsTillAbort(now.Now, states3);
    
    Assert.That(results1, Is.EquivalentTo(new [] { new EmptyReadOperationResult(EOperationReadResult.Success, "" )}));
    Assert.That(results2, Is.EquivalentTo(new [] { new EmptyReadOperationResult(EOperationReadResult.Warning, "" )}));
    Assert.That(results3, Is.EquivalentTo(new [] { new EmptyReadOperationResult(EOperationReadResult.FailedRead, "", EOperationAbortVote.Abort )}));
  }

  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_abort() {
    var now = new TestingUtcDate();
    var repo = ReadTestFactories.Repo(now);
    var runner = ReadTestFactories.Runner(now, repo: repo);
    var composer = ReadTestFactories.Composer(now, cfg, runner, repo);
    
    var states = new List<ReadOperationStateAndConfig> {
      await CreateReadOpStateAndConf(repo, EOperationReadResult.Warning),
      await CreateReadOpStateAndConf(repo, EOperationReadResult.FailedRead),
      await CreateReadOpStateAndConf(repo, EOperationReadResult.Success)
    };
    var results = await composer.RunOperationsTillAbort(now.Now, states);
    
    Assert.That(results, Is.EquivalentTo(new [] { 
      new EmptyReadOperationResult(EOperationReadResult.Warning, "" ),
      new EmptyReadOperationResult(EOperationReadResult.FailedRead, "", EOperationAbortVote.Abort )
    }));
  }
  
  private async Task<ReadOperationStateAndConfig> CreateReadOpStateAndConf(ICtlRepository repo, EOperationReadResult result) 
    => new (
        await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), result.ToString()), 
        new (result.ToString(), new ("* * * * *")));
}


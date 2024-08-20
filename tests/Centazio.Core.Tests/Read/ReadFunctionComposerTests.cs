using centazio.core.Ctl;
using Centazio.Core.Func;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace centazio.core.tests.Read;

public class ReadFunctionComposerTests {

  [Test] public async Task Test() {
    var config = new ReadFunctionConfig(nameof(ReadFunctionComposerTests), [
      new ReadOperationConfig(nameof(ReadFunctionComposerTests), "", 100), 
    ]);
    var stager = new EntityStager(new InMemoryStagedEntityStore(100));
    var composer = new ReadFunctionComposer(config, stager, new TestingUtcDate(), new InMemoryCtlRepository(), new TestReadOpRunner());
    var results = await composer.Run();
    Console.WriteLine(results);
  }
}

public class TestReadOpRunner : IReadOperationRunner {

  public Task<ReadOperationResults> Run(DateTime start, ReadOperationStateAndConfig op) => throw new NotImplementedException();

}
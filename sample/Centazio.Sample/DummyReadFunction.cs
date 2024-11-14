using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample;

public record DummySystemEntyity(Guid Id, string Name);

public class DummyReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, DummySystemApi api) :
    ReadFunction(new(nameof(DummyReadFunction)), stager, ctl) {

  private readonly string EVERY_SECOND_NCRON = "* * * * * *";

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new ReadOperationConfig(new(nameof(DummySystemEntyity)), EVERY_SECOND_NCRON, GetDummySystemEntyityUpdates)
  ]);

  private async Task<ReadOperationResult> GetDummySystemEntyityUpdates(OperationStateAndConfig<ReadOperationConfig> config) => ReadOperationResult.Create(await api.GetDummySystemEntitiesAfter(config.Checkpoint));

}

public class DummySystemApi {

  public Task<List<string>> GetDummySystemEntitiesAfter(DateTime after) {
    var entities = Enumerable.Range(0, Random.Shared.Next(0, 3)).Select(_ => new DummySystemEntyity(Guid.NewGuid(), Guid.NewGuid().ToString()));
    return Task.FromResult(entities.Select(Json.Serialize).ToList());
  }

}
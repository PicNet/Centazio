using Centazio.Core.Ctl;
using Centazio.Core.Stage;

namespace Centazio.Test.Lib.E2E;

public interface ISimulationProvider : IAsyncDisposable {
  Task Initialise(SimulationCtx simulationCtx);
  ICtlRepository CtlRepo { get; }
  IStagedEntityRepository StageRepository { get; }
  ISimulationCoreStorageRepository CoreStore { get; } 
}
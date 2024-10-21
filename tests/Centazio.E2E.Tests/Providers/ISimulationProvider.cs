using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Infra;

namespace Centazio.E2E.Tests.Providers;

public interface ISimulationProvider : IAsyncDisposable {
  Task Initialise(SimulationCtx simulationCtx);
  ICtlRepository CtlRepo { get; }
  IStagedEntityRepository StageRepository { get; }
  CoreStorage CoreStore { get; } 
}
using Centazio.Core;

namespace Centazio.E2E.Tests.Systems;

public interface ISimulationSystem {
  SystemName System { get; } 
  List<ISystemEntity> GetEntities<E>() where E : ISystemEntity;
}
namespace Centazio.E2E.Tests.Systems;

public interface ISystem { ISimulation Simulation { get; } }
public interface ISimulation { void Step(); }
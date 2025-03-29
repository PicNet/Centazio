using DotNet.Testcontainers.Builders;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Centazio.Providers.PostgresSql.Tests;

public class PostgresSqlConnection {

  private readonly IContainer container = new ContainerBuilder()
      .WithImage("postgres:14")
      .WithPortBinding(5432, true)
      .WithEnvironment("POSTGRES_USER", "testuser")
      .WithEnvironment("POSTGRES_PASSWORD", "testpassword")
      .WithEnvironment("POSTGRES_DB", "testdb")
      .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
      .Build();

  public string? connstr { get; private set; }

  // Configure Postgres container

  public async Task<string> Init() {
    if (connstr is not null) return connstr;
    
    await container.StartAsync();
    var port = container.GetMappedPublicPort(5432);
    return connstr = $"Host=localhost;Port={port};Database=testdb;Username=testuser;Password=testpassword";
  }

  public async Task Dispose() {
    if (connstr is null) return;
    connstr = null;
    await container.StopAsync();
    await container.DisposeAsync();
  }

}
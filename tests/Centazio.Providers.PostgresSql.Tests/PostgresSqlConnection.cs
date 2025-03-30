using Testcontainers.PostgreSql;

namespace Centazio.Providers.PostgresSql.Tests;

public class PostgresSqlConnection {

  private readonly PostgreSqlContainer container = new PostgreSqlBuilder().Build();

  public string? ConnStr { get; private set; }

  public async Task<string> Init() {
    if (ConnStr is not null) return ConnStr;
    await container.StartAsync();
    return ConnStr = container.GetConnectionString();
  }

  public async Task Dispose() {
    if (ConnStr is null) return;
    ConnStr = null;
    await container.StopAsync();
    await container.DisposeAsync();
  }
}
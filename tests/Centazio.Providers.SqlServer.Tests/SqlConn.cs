using System.Diagnostics;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Centazio.Providers.SqlServer.Tests;

public class SqlConn {
  
  public static readonly SqlConn Instance = new();
  
  public bool Real { get; }
  public async Task<SqlConnection> Conn() {
    if (String.IsNullOrWhiteSpace(connstr)) { await Init(); }
    return new SqlConnection(connstr);
  }

  private SqlConn(bool real=false) => Real = real;
  
  private MsSqlContainer? container;
  private string connstr = null!;

  private async Task Init() {
    if (Real) {
      var settings = (TestSettings) new SettingsLoader<TestSettingsRaw>().Load("dev");
      var secrets = (TestSecrets) new NetworkLocationEnvFileSecretsLoader<TestSecretsRaw>(settings.SecretsFolder, "dev").Load();
      connstr = secrets.SQL_CONN_STR;
      return;
    }
    container = new MsSqlBuilder().Build();
    await container.StartAsync();
    connstr = container.GetConnectionString();
  }

  // note: This should only be called by TestSuiteInitialiser.cs
  internal async Task Dispose() {
    if (Real) return;
    if (container is null) throw new UnreachableException();
    
    await container.StopAsync();
    await container.DisposeAsync();
  }
}
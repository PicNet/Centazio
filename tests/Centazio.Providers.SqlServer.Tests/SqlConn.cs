using System.Diagnostics;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using centazio.core.tests;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Centazio.Providers.SqlServer.Tests;

public class SqlConn {
  
  public static readonly SqlConn Instance = new();
  
  public bool Real { get; }
  public SqlConnection Conn() => String.IsNullOrWhiteSpace(connstr) ? throw new ArgumentNullException(nameof(connstr)) : new SqlConnection(connstr);
  
  private SqlConn(bool real=false) => Real = real;
  
  private MsSqlContainer? container;
  private string connstr = null!;

  // note: This should only be called by TestSuiteInitialiser.cs
  internal async Task Init() {
    if (!String.IsNullOrWhiteSpace(connstr)) throw new Exception("already initialised");
    
    if (Real) {
      var settings = new SettingsLoader<TestSettings>().Load();
      var secrets = new NetworkLocationEnvFileSecretsLoader<TestSecrets>(settings.SecretsFolder, "dev").Load();
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
    if (container == null) throw new UnreachableException();
    
    await container.StopAsync();
    await container.DisposeAsync();
  }
}
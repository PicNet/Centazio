using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Centazio.Core.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Providers.Aws.Secrets;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Testcontainers.LocalStack;

namespace Centazio.Providers.Aws.Tests.Secrets;

// runs against LocalStack so the tests need no real AWS secrets or write access
public class AwsSecretsLoaderTests : BaseSecretsLoaderTests {

  private LocalStackContainer container;
  private AmazonSecretsManagerClient client;

  [OneTimeSetUp] public async Task Init() {
    container = new LocalStackBuilder()
        .WithImage(TestingDefaults.LOCALSTACK_IMAGE)
        .WithEnvironment("SERVICES", "secretsmanager")
        .Build();
    await container.StartAsync();
    client = new AmazonSecretsManagerClient(new BasicAWSCredentials("test", "test"), new AmazonSecretsManagerConfig { ServiceURL = container.GetConnectionString() });
  }

  [OneTimeTearDown] public async Task Cleanup() {
    client.Dispose();
    await container.DisposeAsync();
  }

  protected override async Task<ISecretsLoader> GetSecretsLoader() =>
    new AwsSecretsLoader(await F.Settings(), client);

  protected override async Task PrepareTestEnvironment(string environment, Dictionary<string, string> secrets) {
    var (id, json) = ((await F.Settings()).AwsSettings.GetSecretsStoreIdForEnvironment(environment), Json.Serialize(secrets));
    try {
      await client.PutSecretValueAsync(new PutSecretValueRequest { SecretId = id, SecretString = json });
    } catch (ResourceNotFoundException) {
      await client.CreateSecretAsync(new CreateSecretRequest { Name = id, SecretString = json });
    }
  }

}

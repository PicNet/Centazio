using Centazio.Providers.Aws.Stage;

namespace Centazio.Providers.Aws.Tests.Stage;

public class DynamoStagedEntityStoreTests {
  [Test] public async Task Go() {
    var dynamo = new DynamoStagedEntityStore();
    await dynamo.Initalise();
    Console.WriteLine("Done");
  }
}
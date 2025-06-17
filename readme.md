# Centazio
#### Data Integration, Workflow and Master Data Platform by PicNet

Centazio is a data integration platform created for .Net developers.  Centazio provides the following features:
- Sophisticated CLI to help you with the management of your cloud resources
- A robust, fault-tolerant framework for building integrations
- A workflow engine to automate manual tasks
- A centralised reporting database that combines data from disparate systems and makes a unified view available
- Guidance on best-practices for data integration

<p align="center">
  <a href="https://picnet.com.au/application-integration-services/">
    <img src="https://www.picnet.com.au/images/centazio-assets/centazio_cli.png" alt="Centazio CLI" width="460">
  </a>
</p>

# Principles
- Zero-Trust:  
  - Expect all systems to go down, expect Centazio to go down.  However, if something goes down, never bring down 
    other parts of the environment.
  - Expect APIs, database schemas, ETL file schemas, etc. to change without warning, expect data to be incorrect and 
    need cleansing.  Never trust, always confirm and apply your own level of validation.
  - Always, automatically and regularly test all assumptions made when dealing with external systems.
- Core Storage:
  - Data from all system should be stored in a central database that is ideal for reporting and business workflows.
  - The core storage database uses business language, independent of the source systems.
  - The core storage is the only source of data when writing to target systems.
- Independence:
  - All systems should be independent and totally ignorant of other systems.
  - All integrations to systems should be done in an isolated fashion.
  - Integration functions should be isolated from other functions.
- Types of Integration Functions:
  - Read: Used when reading data from source systems, this data should be read with as little modification as 
    possible and stored in a staging area to be later cleansed and transformed.  The read function should not worry 
    about validation, cleansing, etc.  
  - Promote: Promoting staging data to Core Storage is done by Promote functions.  All data cleansing, basic 
    aggregation, transformations and language standardisation should be done in the Promote step.
  - Write: Writing data to target systems is done in this step.  All data for writing should come from the Core 
    Storage database and never directly from source systems or staging areas.
  - Other: Any other integration function, such as data aggregation, machine learning, reporting, workflows, etc. 
    Can be done by 'Other' functions.
- Testing Guidelines:
  - Automatically and regularly test everything. And remember the zero-trust principle.
  - Read - Read operations at a minimum should check, for each entity, for each source system:
    - Source system schemas (API, database, text files) have not changed from what is expected.
    - Date formats and time zones in raw data are as expected.
    - Expected admin/category values have not changed from expected.
    - API limits (rate limits) have not changed.
    - Incremental data loading works as expected ('last_updated' > 'date' is respected).
    - API performance is adequate and within agreed SLAs from Vendors.
    - Deleted entities can be retreived from the API, i.e. are soft deleted and available for query.
  - Promote - Promote operations should check:
    - All required data transformations are functioning correctly.
    - All complex business logic usually resides in this step.  This logic requires careful testing.
    - All source system language is tranformed to the business's ubiquitous language upon promotion.
    - All required data cleansing is applied during the operation.
    - All sensitive data is correctly handled.
    - All date/time transformations are applied correctly.  Centazio Core Storage should only store UTC dates for 
      all datetime fields.
  - Write - Write operations should check:
    - Write tests should read data back after writing to ensure the written data is as expected.
    - Target system schemas (API, database, text files) have not changed from what is expected.
    - Date formats and time zones are as expected.
    - All Core Storage admin/categorical field values are supported by the target system.
    - API limits (rate limits) have not changed.
    - API performance is adequate and within agreed SLAs from Vendors.
  - Other - Other operation tests should be customised to their required functionality.  Some common scenarios include:
    - Testing that 'Data Validation' operations correctly delete entities that do not support soft-deletes in source 
      systems.
    - Test that required emails or workflows are executed as expected.
    - Test that data aggregation works and is mathematically correct.
    - Test machine learning model retraining results in achieving the benchmark loss-function levels.
    - Test reporting tasks generate reports and that reporting data is as expected.

# Serverless / Independence
The principles described above are ideal for Serverless architectures.  Each system and operation type (Read,Promote,
Write) should be in its own totally isolated Serverless function (we could even isolate each entity type if we 
wanted to).  For instance; reading new Incidents from the Incident Management System, and then creating 
corresponding Alerts in the Notification System would be broken down into the following Serverless functions:

- IncidentMgtSysReadFunction
- IncidentMgtSysPromoteFunction
- NotificationSystemWriteFunction

Each of these functions are independent of each other, can be independently developed, tested, documented, etc.  
They are also fault-tolerant of failures in any other function.

It is common for the Read function to be triggered by a timer, the other functions can either be triggered by timers 
or through messages sent via queues.  This messaging infrastructure
is provided by Centazio.  However, being a fault-tolerant centric system, we should never solely rely on these 
notifications/queues, we should always have timer based triggers to ensure that the integration will eventually be 
called.

Centazio currently supports Self Hosting (own server), AWS Lambda and Azure Functions based deployments for Centazio 
integrations.  All supporting infrastructure such as logging, alerting, queues, events, notifications, email 
providers, networking, etc. can also be managed by Centazio or you can use your own cloud management and DevOps 
pipelines to control.

# Prerequisites
- [aws cli](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
- todo GT: fill in

# Getting Started

This simple getting started guide will guide you in creating a simple one-way integration between two systems:
- [Google AppSheet](https://www.appsheet.com/)
- [ClickUp](https://clickup.com/)

## Installing Centazio

- `dotnet tool install --prerelease --global Centazio.Cli`
- Create a secrets file anywhere on your computer (outside of the current git directory).  Name this file `dev.env`

## Set up ClickUp
- Create a free ClickUp account: https://clickup.com/
- Create PAT: Avatar -> Settings -> Apps -> Generate: Copy the generated token
- Add a line in your secrets file (`dev.env`) with your PAT, example:
  `CLICKUP_TOKEN=pk_12345678_ABCDEFGHIJKLMNOPQRSTUVWXYZ123456`
- Create a list to todo items, and copy the list ID from URL (the trailing number)

## Set up AppSheet
- https://www.appsheet.com/ -> Get Started
- Create an app -> Settings -> Integrations -> Enable (and copy App ID) 
- Create Application Access Key (and copy key)
- Save App (CTRL S)
  - Add aline to your serets file (`dev.env`) with the api key, example:
  `APPSHEET_KEY=V2-<abcde-abcde-abcde-abcde-abcde-abcde-abcde-abcde>`

## Start Coding

### Setup the Centazio solution
- `centazio gen sln CentazioTesting`
- `cd CentazioTesting`
 
For now notice the `CentazioTesting.Shared` project that has been created and feel free to look around the generated 
code.  The classes generated include:
- `Assembly.cs`: General assembly details, including global includes.
- `CoreEntityTypes.cs`: This are your Core Storage entities and corresponding DTOs.  Including an example entity 
  showing the Dto pattern that Centazio recommends.
- `CoreStorageDbContext.cs` / `CoreStorageRepository.cs`: An SQLite implementation of the Core Storage repository.  
  This can be replaced with any other supported provider (or your own).
- `Secrets.cs`: A sample secrets file that is used to deserialise your `dev.env` secrets file.  Notice this file 
  extends `CentazioSecrets` which contains standard Centazio secrets.
- `Settings.cs`: A sample settings file that is used to deserialise your `settings.json` settings file. Notice this 
  file extends `CentazioSettings` which contain Centazio standard settings.

### Reading ClickUp Items
Let's create a simple data ingestion function that will read new todo items added to your ClickUp list.
- `centazio gen func -type read -system ClickUp`

This command will add a Read Function to your solution and includes the following files:
- `ClickUpReadFunction.cs`: Where all the read logic is implemented. Note: This is the only file that is strictly 
  necessary.  All other types could be placed in the shared project, or perhaps a new shared project for the 
  specific target system.
- `ClickUpTypes.cs`: Any types required to assist in serialisation with the target system.
- `ClickUpApi.cs`: Any api access implementation required to access data from the external system.

You will then need to implement the read functionality as required.  Let's implement:

#### ClickUpTypes.cs:
```
public static class ClickUpConstants {
  public static readonly SystemName ClickUpSystemName = new (nameof(ClickUpSystemName));
  
  public static readonly SystemEntityTypeName ClickUpTaskEntityName = new(nameof(ClickUpTask));
}

[IgnoreNamingConventions] 
public record ClickUpTask(string id, string name, ClickUpTaskStatus status, string date_updated) : ISystemEntity {

  
  
  public SystemEntityId SystemId { get; } = new(id);
  public DateTime LastUpdatedDate => UtcDate.FromMillis(date_updated);
  public string DisplayName => name;
  
  [JsonIgnore] public bool IsCompleted => status.status == ClickUpApi.CLICK_UP_COMPLETE_STATUS;
  
  public ISystemEntity CreatedWithId(SystemEntityId newid) => this with { id = newid.Value };
  public object GetChecksumSubset() => new { id, name, status };

}

public record ClickUpTaskStatus(string status);
```

#### `ClickUpApi.cs`:
```
public class ClickUpApi(Settings settings, Secrets secrets) {

  public static readonly string CLICK_UP_OPEN_STATUS = "to do";
  public static readonly string CLICK_UP_COMPLETE_STATUS = "complete";
  
  private static HttpClient? http; 
  
  public async Task<List<TaskJsonAndDateUpdated>> GetTasksAfter(DateTime after) {
    // https://developer.clickup.com/reference/gettasks
    var json = await Query($"list/{settings.ClickUp.ListId}/task?archived=false&order_by=updated&reverse=true&include_closed=true&date_updated_gt={after.ToMillis()}");
    return Json.SplitList(json, "tasks")
        .Select(taskjson => new TaskJsonAndDateUpdated(taskjson, UtcDate.FromMillis(taskjson, @"""date_updated"":""([^""]+)""")))
        // it is possible for the ClickUp API to include some tasks even though we specify date_updated_gt, so filter manually
        .Where(t => t.LastUpdated > after)
        .OrderBy(t => t.LastUpdated)
        .ToList();
  }
  
  public async Task<string> CreateTask(string name) {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    
    var resp = await Client.PostAsync($"list/{settings.ClickUp.ListId}/task", Json.SerializeToHttpContent(new { name }));
    var json = await resp.Content.ReadAsStringAsync();
    var node = JsonNode.Parse(json) ?? throw new Exception();
    return node["id"]?.ToString() ?? throw new Exception();
  }

  public async Task UpdateTask(string id, string name) => await UpdateImpl(id, new { name });
  public async Task OpenTask(string id) => await UpdateImpl(id, new { status = CLICK_UP_OPEN_STATUS });
  public async Task CloseTask(string id) => await UpdateImpl(id, new { status = CLICK_UP_COMPLETE_STATUS });
  public async Task DeleteTask(string id) => await Client.DeleteAsync($"task/{id}");

  // https://developer.clickup.com/reference/updatetask
  private async Task UpdateImpl(string id, object content) => await Client.PutAsync($"task/{id}", Json.SerializeToHttpContent(content));
  

  private async Task<string> Query(string path) {
    using var request = await Client.GetAsync(path);
    return await request.Content.ReadAsStringAsync();
  }

  private HttpClient Client => http ??= new HttpClient { 
    BaseAddress = new Uri(settings.ClickUp.BaseUrl),
    DefaultRequestHeaders = { {"Authorization", secrets.CLICKUP_TOKEN }, }
  };

}

public record TaskJsonAndDateUpdated(string Json, DateTime LastUpdated);
```

#### ClickUp Read Function
```
public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api) : ReadFunction(ClickUpConstants.ClickUpSystemName, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(ClickUpConstants.ClickUpTaskEntityName, CronExpressionsHelper.EveryXSeconds(5), GetUpdatedTasks)
  ]);

  private async Task<ReadOperationResult> GetUpdatedTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    var tasks = await api.GetTasksAfter(config.Checkpoint);
    var last = tasks.LastOrDefault()?.LastUpdated;
    return CreateResult(tasks.Select(t => t.Json).ToList(), last);
  }
}
```

### Promoting ClickUp Items
Now that our click data is in the staging database, it is time to promote it to the master data or core storage
database.  The core storage database should be the combined intelligence of all your systems in one centralised
location/database.
* `centazio gen func -type promote -system ClickUp`

The code its explanation can be found [here](https://github.com/PicNet/Centazio/blob/master/sample/Centazio.Sample.ClickUp/).  

### Writing Items to Google AppSheet
We now have some data in our master data or core storage database.  Other systems can now be fed that data.
`centazio gen func -type write -system AppSheet`

The code its explanation can be found [here](https://github.com/PicNet/Centazio/tree/master/sample/Centazio.Sample.AppSheet).

### The Rest
Please see the [Centazio.Sample](https://github.com/PicNet/Centazio/tree/master/sample) project in the [Centazio 
GitHub repository](https://github.com/PicNet/Centazio/) for a complete two-way implementation of this sample 
integration.

## Core Centazio Developer Concepts:

The following are more details that you will need as you progress with Centazio and encounter more complex scenarios.  

### Serialisation / Deserialisation / Mapping
Data integration is all about getting data from one source, converting it and writing it to another target.  This 
source or target could be an API, database, files, etc. and the quality and reliability of the data and its schema 
cannot be trusted.  As such, you should *not* assume that fields exist, have valid values, etc. when reading data 
from an external source.  A pattern used in Centazio to handle this is to have the concept of `Dto` internal objects 
which are then converted to their expected types with all required validation.  

This pattern has the following characteristics:
- The Main record:
  - should have no public constructors (including default constructor) to ensure that serialisation libraries or ORM 
    libraries cannot create these objects
  - all fields should then either be set by this private constructor or by init only required fields
  - should use strongly typed ids, and other value types
- The Dto record:
  - shold be an inner class of the main record type, Centazio uses the name `Dto` for all of its Dto implementations.
  - should implement the `IDto<MainRecordType>` interface
  - all fields should be nullable
  - must implement the `IDto<MainRecordType>.ToBase` method with all required validations and conversions to 
    strongly typed value objects

Example: 
  
```
public record StagedEntity {
  public Guid Id { get; }
  public SystemName System { get; }
  public ObjectName Object { get; }

  private StagedEntity(Guid id, SystemName system, ObjectName object) {
    (Id, SystemName, Object) = (id, system, object);
  } 

  public record Dto : IDto<StagedEntity> {
    public string? Id { get; init; }
    public string? System { get; init; }
    public string? Object { get; init; }

    public StagedEntity ToBase() => new StagedEntity(
      String.IsNullOrWhiteSpace(Id) 
          ? throw new ArgumentNullException(nameof(Id)) 
          : Guid.Parse(Id),
      String.IsNullOrWhiteSpace(System) 
          ? throw new ArgumentNullException(nameof(System)) 
          : new SystemName(System),
      String.IsNullOrWhiteSpace(Object) 
          ? throw new ArgumentNullException(nameof(Object)) 
          : new ObjectName(Object)
    );
  }
}
```

### Projects / Assemblies / Functions
Centazio makes it possible to have multiple functions per assembly.  However, if you are working in AWS you may want 
to avoid this and just have a single function per assembly.  This will more align with the way AWS Lambdas are 
deployed.  Multi-function assembly is still supported, you will just need to specify the function you want to deploy 
to AWS from within the Assembly.

If you are using Azure, then a multi-function assembly is more akin to Function Apps and will be deployed together 
to Azure as one deployment unit.

### IIntegrationBase

Every generated Centazio project needs a class that imlements the `IIntegrationBase` interface.  This class has two 
responsibilities, one is to register all DI resources used by the functions in the project and an initialise method, 
used to initialise any resources used by functions in the given assembly:

```
public interface IIntegrationBase {
  void RegisterServices(CentazioServicesRegistrar registrar);
  Task Initialise(ServiceProvider prov);
}
```

### Operations

Every Centazio Function is broken up by operations.  Each operation can control its timer intervals, and is tracked 
independently by the control tables.  Operations allow a clear way of separating entity specific operations for 
specific systems.  Say we are reading incident management data from the 'Incident Management System'.  We could have 
the following `ImsReadFunc.cs` function:

```
public class ImsReadFunc(ImsApi api) : ReadFunction(sysname, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => 
      new FunctionConfig([
    new ReadOperationConfig('Incs', '* * * * * *', GetIncidents),
    new ReadOperationConfig('Alerts', '* * * * * *', GetAlerts),
    new ReadOperationConfig('Emails', '* * * * * *', GetSentEmails)
  ]);
  ...
}
```

Here we break down reading the IMS data into three operations, one for reading Incidents, another for Alerts and 
finally an operation for reading Sent Emails.

You can manage this separation of operations how best suits your project.  Another common pattern is to have one 
function per system, per operation, per entity type.  This could be overkill, but sometimes this level of separation 
of concerns can add benefits:

```
public class ImsReadIncidentsFunc(ImsApi api) 
    : ReadFunction(sysname, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => 
      new FunctionConfig([
    new ReadOperationConfig('Incs', '* * * * * *', GetIncidents)    
  ]);
  ...
}
```

### Configuration / Settings

Your projects should define their own Settings types for serialisation.  Your main settings file should inherit from 
`CentazioSettings` and add any other settings that your application needs.  

Centazio will manage this serialisation/deserialisation, deployment to cloud environments, dependency injection, etc. 

### Secrets

Your projects should define their own Secrets type for serialisation.  Your main settings file should inherit from 
`CentazioSecrets` and add any other settings that your application needs.  

Centazio will manage this serialisation/deserialisation, deployment to cloud environments, dependency injection, etc.

### Configuring Azure Secrets KeyVault Access

To enable KeyVault access for your Azure Function, follow these steps:

1. **Enable Managed Identity**
    - Navigate to the Azure Portal
    - Go to your Function App settings
    - Select "Identity" from the left menu
    - Under the "System assigned" tab, switch the Status to "On"
    - Click "Save" to confirm
    - Click on Azure Role Assignment
    - Then Add the KeyVaultSecretUser Role or any role with read list and value for secrets.

2. **Configure KeyVault Access**
    - Go to your Azure KeyVault resource
    - Select "Access control (IAM)" from the left menu
    - Click "+ Add" and select "Add role assignment"
    - Choose "Key Vault Secrets User" role
    - Under "Members", select "Managed identity"
    - Select your function app from the list of managed identities
    - Click "Review + assign" to save the changes

## Common CLI Commands

- Install: `dotnet tool install --prerelease --global Centazio.Cli`
- Update: `dotnet tool update --prerelease --global Centazio.Cli`
- Remove: `dotnet tool uninstall --global Centazio.Cli`, `dotnet tool uninstall --local Centazio.Cli`
- Check: `dotnet tool list --global`, `dotnet tool list --local`
- Generate Solution: `centazio gen sln YourSolutionName`
- Generate Function: `centazio gen func YourFunctionName`
- Run Local Host: `centazio host run Centazio.Sample` - This will run the AppSheet / ClickUp integration sample project
- Generate Azure Function: `centazio az func generate YourFunctionName` - Generate an Azure Functions wrapper for 
  your function 
- Generate AWS Lambda Function:`centazio aws func generate YourFunctionName` - Generate an AWS Lambda Function 
  wrapper for your function
- Deploy Azure Function: `centazio az func deploy YourFunctionName` - Package and deploy your function to Azure
- Deploy AWS Lambda Function: `centazio aws func deploy YourFunctionName` - Package and deploy your function to AWS

For full details of the supported Centazio CLI commands, simply run `centazio --help`

# Sponsors
<a href="https://picnet.com.au" style="color:inherit;text-decoration: none">
Centazio is proudly sponsored by PicNet.
<p align="center"><img src="https://www.picnet.com.au/images/centazio-assets/picnet.jpg" alt="PicNet" width="250"></p>
</a>
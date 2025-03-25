# Centazio
#### Data Integration, Workflow and Master Data Platform by PicNet

Centazio is a data integration platform created for .Net developers.  Centazio provides the following features:
- Sophisticated CLI the help you with the management of your cloud resources
- A robust, fault-tolerant framework for building integrations
- A workflow engine to automate manual tasks
- A centralised reporting database that integrates all your data from disparate systems
- Guidance on best-practices for data integration

<p align="center">
  <a href="https://picnet.com.au/application-integration-services/">
    <img src="https://www.picnet.com.au/images/centazio-assets/centazio_cli.png" alt="Centazio CLI" width="460">
  </a>
</p>

# Principles
- Zero Trust:  
  - Expect all systems to go down, expect Centazio to go down.  However, if something goes down, never bring down other parts of the environment
  - Expect APIs, database schemas, ETL file schemas, etc. to change without warning, expect data to be incorrect and need cleansing.  Never trust, always confirm and apply your own level of validation
  - Always automatically and regularly test all assumptions made when dealing with external systems.
- Core Storage:
  - Data from all system should be stored in a central database that is ideal for reporting and business workflows.
  - The core storage database uses business language, independent of the source systems
  - The core storage is the only source of data when writing to target systems
- Independence:
  - All systems should be independent and totally ignorant of other systems
  - All integrations to systems should be done in an isolated fashion
  - Integration steps should be isolated from other steps
- Main Steps:
  - Read: When reading data from a source system, this data should be read with as little modification as possible.
    This raw data can be saved in a staging area to be later cleanse.  The read step should not worry about validation,
    cleansing, etc.
  - Promote: Promoting staging data to Core Storage is done by Promote functions.  All data cleansing, basic aggregation,
    transformation and language standadisation should be done in the Promote step.
  - Write: Writing data to target systems is done in this step.  All data for writing should come from the Core Storage
    database and never directly from source systems.
  - Other: Any other integration function, such as data aggregation, machine learning, reporting, workflows, etc. Can be 
    done by 'Other' functions.
- Testing Guide Lines
  - Automatically and regularly test everything
  - Read - Read operations at a minimum should check, for each entity, for each source system:
    - Source system schemas (API, database, text files) have not changed from what is expected
    - Date formats and timezones in raw data are as expected
    - Expected admin/catgory values have not changed from expected
    - API limits (rate limits) have not changed
    - Incremental data loading works as expected ('last_updated' > 'date' is respected)
    - API performance is adequate and within agreed SLAs from Vendors
    - Deleted entities can be retreived from the API, i.e. are soft deleted and available for query
  - Promote - Promote operations should check:
    - All required data transformations are functioning correctly
    - All source system languge is tranformed to the business's ubiquitous language upon promotion
    - All required data cleansing is applied during the operation
    - All sensitive data is correctly handled
    - All date/time transformations are applied correctly.  Centazio Core Storage should only ever store UTC dates for all datetime fields
  - Write - Write operations should check:
    - Write tests should read data back after writing to ensure the data is as expected
    - Target system schemas (API, database, text files) have not changed from what is expected
    - Date formats and timezones are as expected
    - All Core Storage admin/categorical field values are supported by the target system
    - API limits (rate limits) have not changed    
    - API performance is adequate and within agreed SLAs from Vendors
  - Other - Other operation tests should be customised to their required functionality.  Some common scenarios include:
    - Testing that 'Data Validation' operations correctly delete entities that do not support soft-deletes in source systems
    - Test that required emails or workflows are executed as expected
    - Test that data aggregation works and is mathematically correct
    - Test machine learning model retraining results in achieving the benchmark loss-function levels
    - Test reporting tasks generate reports and that reporting data is as expected

# Serverless / Independence
The principles descibed above are ideally serviced by using Serverless architectures.  Each system and operation type (Read,Promote,Write) should be in its own totally isolated Serverless function (we could even isolate each entity type if we wanted to).  For instance; reading new Incidents from the Incident Management System, and then creating corresponding Alerts in the Notification System would be broken down into the following Serverless functions:

- IncidentMgtSysReadFunction
- IncidentMgtSysPromoteFunction
- NotificationSystemWriteFunction

Each of these functions are independent of each other, can be independently developed, tested, documented, etc.  They are also fault tollerant of failures in any other function.

It is common for the Read function to be triggered by a timer, the other functions can either be triggered by timers or through messages sent via queues.  This messaging infrastructure
is supplied by Centazio.  However, being a fault-tollerant centric system, we should never solely rely on these queues, we should always have timer based triggers to ensure that the integration will eventually be called.

Centazio currently supports Self Hosting (own server), AWS Lambda and Azure Functions based deployments for Centazio integrations.  All supporting infrastructure such as logging, alerting, queues, events, notifications, email providers, networking, etc. Can also be managed by Centazio or you can use your own cloud management and DevOps pipelines to control.

# Getting Started

This simple getting started guide will guide you in creating a simple integration between two systems:
- [Google AppSheet](https://www.appsheet.com/)
- [ClickUp](https://clickup.com/)

## Installation Centazio

- `dotnet tool install centazio`
- Create a secrets file anywhere on your computer (outside of a git directory).  For now lets name this `dev.env`
- TODO: add more details

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
code.  We will go over each generated component in detail later on in this document.

### Reading ClickUp Items
Let's create a simple data ingestion task that will read new todo items added to your ClickUp list.
`centazio gen func -t r -s ClickUp`
or
`centazio gen func -t read -s ClickUp`
or
`centazio gen func -type read -system ClickUp`
TODO

### Promoting ClickUp Items
Now that our click data is in the staging database, it is time to promote it to the master data or core storage
database.  The core storage database should be the combined intelligence of all your systems in one centralised
location/database.
`centazio gen func -t p -s ClickUp`
or
`centazio gen func -t promote -s ClickUp`
or
`centazio gen func -type promote -system ClickUp`
TODO

### Writing Items to Google AppSheet
We now have some data in our master data or core storage database.  Other systems can now be fed that data.
`centazio gen func -t w -s AppSheet`
or
`centazio gen func -t write -s AppSheet`
or
`centazio gen func -type write -system AppSheet`
TODO

### The Rest
Please see the [Centazio.Sample](https://github.com/PicNet/Centazio/tree/master/sample) project in the [Centazio GitHub repository](https://github.com/PicNet/Centazio/) for a complete two-way implementation of this sample integration.

## Core Centazio Developer Concepts:

The following are more details that you will need as you progress with Centazio and encounter more complex scenarios.  

### Serialisation / Deserialisation / Mapping
Data integration is all about getting data from one source, converting it and writing it to another target.  This source or target could be an API, database, files, etc. and the quality and reliability of the data and its schema cannot be trusted.  As such, you should *not* assume that fields exist, have valid values, etc. when reading data from an external source.  A pattern used in Centazio to handle this is to have the concept of `Dto` internal objects which are then converted to their expected types with all required validation.  

This pattern has the following characteristics:
- The Main record:
  - should be sealed
  - should have no public constructors (including default constructure) to ensure that serialisation libraries or ORM libraries cannot create these objects
  - all fields should then either be set by this private constructor or by init only required fields
  - should use stronly typed ids, and other value types
- The Dto record:
  - shold be an inner class of the main record type, Centazio uses the name `Dto` for all of its Dto implementations.
  - should implement the `IDto<MainRecordType>` interface
  - all fields should be nullable
  - must implement the `IDto<MainRecordType>.ToBase` method with all required validations and conversions to strongly typed value objects

Example: 
  
```
public sealed record StagedEntity {
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
Centazio makes it possible to have multiple functions per assembly.  However, if you are working in AWS you may want to avoid this and just have a single function per assembly.  This will more align with the way AWS Lambdas are deployed.  Multi-function assembly is still supported, you will just need to specify the function you want to deploy to AWS from within the Assembly.

If you are using Azure, then a multi-function assembly is more akin to Function Apps and will be deployed together to Azure as one deployment unit.

### IIntegrationBase

Every generated Centazio project needs a class that imlements the `IIntegrationBase` interface.  This class has two responsibilities, one is to register all DI resources used by the functions in the project and an initialise method, used to initialise any resources used by functions in the given assembly:

```
public interface IIntegrationBase {
  void RegisterServices(CentazioServicesRegistrar registrar);
  Task Initialise(ServiceProvider prov);
}
```

### Operations

Every Centazio Function is broken up by operations.  Each operation can control its timer intervals, and is tracked independently by the control tables.  Operations allow a clear way of separating entity specific operations for specific systems.  Say we are reading incident management data from the 'Incident Mangement System'.  We could have the following `ImsReadFunc.cs` function:

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

Here we break down reading the IMS data into three operations, one for reading Incidents, another for Alerts and finally an operation for reading Sent Emails.

You can manage this separation of operations how best suits your project.  Another common pattern is to have one function per system, per operation, per entity type.  This could be overkill, but sometimes this level of separation of concerns can add benefits:

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

### Configuration

TODO

### Secrets

TODO

## Common CLI Commands

- Install: `dotnet tool install --prerelease --global Centazio.Cli`
- Update: `dotnet tool update --prerelease --global Centazio.Cli`
- Remove: `dotnet tool uninstall --global Centazio.Cli`, `dotnet tool uninstall --local Centazio.Cli`
- Check: `dotnet tool list --global`, `dotnet tool list --local`
- `centazio host run Centazio.Sample` - This will run the AppSheet / ClickUp integration sample project
- `centazio az func generate Centazio.TestFunctions` - Generate dummy functions (can be many) Azure wrapper 
- `centazio az func generate Centazio.TestFunctions EmptyFunction` - Generate dummy function (single) AWS wrapper
- `centazio aws func deploy Centazio.TestFunctions` - Package and deploy dummy functions to AWS
- `centazio az func deploy Centazio.TestFunctions` - Package and deploy dummy functions to AWS

# Sponsors
<a href="https://picnet.com.au" style="color:inherit;text-decoration: none">
Centazio is proudly sponsored by PicNet.
<p align="center"><img src="https://www.picnet.com.au/images/centazio-assets/picnet.jpg" alt="PicNet" width="250"></p>
</a>
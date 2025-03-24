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
  - Expect all systems to go down, expect Centazio to go down.  However, if something goes down, never bring 
    down other parts of the environment
  - Expect APIs to change without warning, expect data to be incorrect and need cleansing.  Never trust, always confirm
    and apply your own level of validation
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

# Serverless / Independence
The principles descibed above are ideally serviced by using Serverless architectures.  Each system and step should be its
own totally isolated serverless function.  For instance, reading data from System1 to write to System 2 will be broken 
down into the following Serverless functions:

- System1ReadFunction
- System1PromoteFunction
- System2WriteFunction

Each of these functions are independent of each other, can be independently developed, tested, documented, etc.  They
are also fault tollerant of failures in any other function.

Centazio currently supports Self Hosting, AWS Lambda and Azure Functions based deployments for Centazio Functions.

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

## Serialisation / Deserialisation / Mapping
Data integration is all about getting data from one source, converting it and writing it to another target.  This source
or target could be an API, database, files, etc. and the quality and reliability of the data and its schema cannot be 
trusted.  As such, you should *not* assume that fields exist, have valid values, etc. when reading data from an external
source.  A pattern used in Centazio to handle this is to have the concept of `Dto` objects which are then converted
to their expected types with all required validation.  

This pattern has the following characteristics:
- Main record type should have a private constructor with the minimal set of fields required for creation of the record
- Since there is no private primary constructors, all properties must be declared (with no setters)
```
public sealed record StagedEntity {
  public Guid Id { get; }
  public SystemName System { get; }
  public ObjectName Object { get; }
  ...
}
```

- A `public static` factory `Create` method needs to be added to set these minimal fields.  This factory method should
do all required data and field validation.  However, complex validations should be avoided and custom factory methods
should be provided to provide support for different creation scenarios.
```
public sealed record StagedEntity {
  ...
  public static StagedEntity Create(SystemName system, ObjectName obj, DateTime staged, ValidString data, ValidString checksum) => new(Guid.CreateVersion7(), source, obj, staged, data, checksum);
  ...
```

- Any change of internal state should be done using mutator methods that return a new instance of the mutated object
- Fields that do require mutation will need `private` `init` only setters
- These mutators and factory methods should handle all internal infrastructure logic such as setting the
created/updated dates, etc.
```
public sealed record StagedEntity {
  ...
  public DateTime? DatePromoted { get; private init; }
  ...
  public StagedEntity Promote(DateTime promoted) => this with { DatePromoted = promoted };
  ...
```

- Deserialise and serialisation should be done via an inner `Dto` class
- This class must be an inner class to access the private init only setters
- This class needs a parameterless constructor and all fields must be nullable
```
public sealed record StagedEntity {
  ...
  public record Dto {
    public Guid? Id { get; init; }
    ...
  }
```

- Explicit cast operator overrides can then be used to convert between this `Dto` and main record type.  
- All field validation must happen in these methods
```
  public sealed record StagedEntity {
  ...
  public record Dto {
    public static explicit operator StagedEntity(Dto dto) => new(...) { ... };
    public static explicit operator Dto(StagedEntity se) => new { ... };    
  }
```

- This pattern allows Enums to be serialised/deserialised as strings and converted to Enums in the converter methods
and other more complex transformations and validations.
- See `StagedEntity.cs` for an implementation example of this pattern

- Example consumption of this pattern:
```
// serialise StagedEntity as StagedEntity.Dto
JsonSerializer.Serialize(staged.Select(e => (StagedEntity.Dto)e));

// deserialise from unsafe StagedEntity.Dto to StagedEntity
JsonSerializer.Deserialize<List<StagedEntity.Dto>>(json).Select(e => (StagedEntity) e).ToList()
```

- For unit tests that require modifying inner state to test edge cases then this `Dto` can also be used to circumvent
these safety measures:
```
var x = (StagedEntity) new StagedEntity.Dto { ... };
```

## Functions

A central facet of Centazio is the concept of 'Functions'.  The three main common functions are read, promote and write.
Each source system should have a read and promote function.  Each target (or sink) system should have a write function.

Read: Read functions read data from the source system, via an api, database, etc.  This data is written in its raw format
to the staging area.

Promote: Promote functions read newly added data from the staging area and 'promote' this data into the core storage.

Write: Write functions read newly updated data in core storage to the target systems.

### Function Technical Details

#### FunctionRunner:

The `FunctionRunner` class is the main controller that executes a function.  This class will be called by the host
container, whether that is an AWS Lambda Function, Azure Function or a local process.  The `FunctionRunner` needs to
be initialised with the following components:

- An instance of `AbstractFunction` which is the function to be executed
- An instance of `IOperationRunner` which is a specialised implementation of this interface that knows how to 
  perform the required operations of this function

The `RunFunction` method is the method that the host will call to execute this function and all child operations.


#### AbstractFunction<C, R>:

todo: expand 

## Common Commands

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
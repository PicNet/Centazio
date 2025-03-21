# Centazio
#### Data Integration, Workflow and Master Data Platform by PicNet

Centazio is a data integration platform created for .Net developers.  Centazio provides the following features:
* Sophisticated CLI the help you with the management of your cloud resources
* A robust, fault-tolerant framework for building integrations
* A workflow engine to automate manual tasks
* A centralised reporting database that integrates all your data from disparate systems

<p align="center">
  <a href="https://picnet.com.au/application-integration-services/">
    <img src="https://www.picnet.com.au/images/centazio-assets/centazio_cli.png" alt="Centazio CLI" width="460">
  </a>
</p>

# Getting Started

## Installation

`dotnet tool install centazio`

## Reading Data
Let's create a simple data ingestion task:

`centazio gen soln CentazioSolution`
`cd CentazioSolution`
`centazio gen proj ReadFunction 

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
- `centazio host run Centazio.Sample` - This will run the AppSheet / ClickUp integration sample project
- `centazio az func generate Centazio.TestFunctions` - Generate dummy functions (can be many) Azure wrapper 
- `centazio az func generate Centazio.TestFunctions EmptyFunction` - Generate dummy function (single) AWS wrapper
- `centazio aws func deploy Centazio.TestFunctions` - Package and deploy dummy functions to AWS
- `centazio azure func deploy Centazio.TestFunctions` - Package and deploy dummy functions to AWS

# Sponsors
<a href="https://picnet.com.au" style="color:inherit;text-decoration: none">
Centazio is proudly sponsored by PicNet.
<p align="center"><img src="https://www.picnet.com.au/images/centazio-assets/picnet.jpg" alt="PicNet" width="250"></p>
</a>

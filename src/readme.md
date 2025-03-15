# Todo
- aws/azure wrappers:
  - Azure:
    - fix Log.Logger, currently only injected ILogger<> works
      - this will somewhat resolve the issue: var config = LogInitialiser.GetFileConfig(dir: @"C:\home\LogFiles\Application\Functions\Function\EmptyFunction");
      - however, we now get duplicate logs and they are a delayed
    - application insights
    - key vault, see: https://claude.ai/chat/2538ccd6-d0e3-49a2-a155-74972ddb606b
    - currently settings are done using the `settings.json` files, these are hard to change in Azure, so perhaps find 
        a way of converting to standard .Net settings so they can be set using the Azure UI
    - support overriding function specific settings like service plan, etc 
  - Aws:
    - https://docs.aws.amazon.com/lambda/latest/dg/csharp-image.html
    - image: public.ecr.aws/lambda/dotnet:9
    - currently hardcodes 1 minute trigger
- function-to-function triggers:
  - aws/azure
  - the triggered function should get as a parameter the objects that changed
  - functions currently work out their triggers, we should allow using FunctionConfig to override this
  - read vs promote/write should now have separate default refresh rates since read can trigger others
- `centazio host run Centazio.Sample` is showing lots of `function is already running, ignoring run`
- aws/azure should have their own defaults (like default cron)
- defaults should also specify default cron for read/promote/write functions
- function cron trigger should translate to AWS/Azure trigger, no need to have two levels of cron triggers
- use picnet github to host publicly
- nuget all code
- cli:
  - install as `dotnet tool`
    - have a `cetazio dev-mode` command to switch between nuget and development mode (with git cloned centazio libs)
  - simulate should not open a new window when running just one function
  - code generators for new solution/project/integration/function
  
- remove all ugly usage of auto Dto conversions, remove DtoHelpers, use manual mapping only (settings already done)
- create a good set of architectural policies that can be validated using NetArchTest, see: https://dateo-software.de/blog/netarchtest
  - many tests in `Centazio.Core.Tests.Inspect` namespace can be improved using NetArchTest
- check that all provider tests in Centazio.Core have Provider counterparts
- support cloud based config timer triggers, have a separate mode?
- new `Centazio.TestFunctions` that implement read/promote/write so we can fully test all workflows
- set up azure devops testing pipeline
- documentation and blogs

# Developer Guidelines:

## Code Style 
- 2 tab spaces
- Braces on same line
- Avoid long camel-case names.  Methods should be short, so having ugly variableNamesThatAddNoValueInAShortContext 
    should be avoided. 
- Class names and method names can be descriptive, allowing for the above short/concise variables
- Use implicitly typed variables where possible
- Use `is null` / `is not null` instead of `== null` / `!= null`

## Logging
- Logging should prefer lower case.  When describing code (classes, etc.) then proper casing is allowed
- Test all structured logging and add any 'clean' versions of structured output to `LogInitialiser.cs` also override
    class `ToString()` method where appropriate to support clean structured logging
- Logging of exceptions should use the exception override logging methods: `Log.Information(exception, message)` and
    not passing the exception as an `{@Exception}` parameter
- Consider that logs are written as follows:
  - {"@t":\<timestamp>,"@mt":\<message template>,\<property pairs>}
  - So try to keep the message template clean.  Example:
    - `Log.Information("error occurred {@SystemState} {@LifecycleStage} {@ErrorMessage}")`
    - This has a very brief lowercase message 'error occurred'
    - Then followed by structured objects being specific of their `Class` names
    - Try not to mix messages and objects like `"This @ObjectName is an object inside the message"`

## Exceptions
- Exception messages should be lower-case.  When describing code (classes, etc.) then proper casing as allowed
- Exception messages should be very concise, example: `already initialised` is better than `class XXX should only be
    initialised once, and it appears that it is already initialsed, bad boy`.  The latter adds no additional value
    only opportunity for typos and clutter
- Any parameters shown in exceptions should be inside square brackets, eg: `throw new Exception(@"value is invalid [{Value}]");`
- Prefer using helpers, such as `ArgumentException.ThrowIf` rather than `if (x is null) throw new ArgumentException`

## Unit Testing
- All complex logic should be unit tested
- Simple logic should not be unit tested, this includes records, simple properties, etc
- Special care is needed when testing date related operations
  - The `TestingUtcDate.DoTick()` method can be used to increment the testing `Now` value to ensure date handling
  is being done correctly
- Extension methods encourage testing of otherwise private methods so use them
- Add unit test project as `[assembly: InternalsVisibleTo("<test_project>")]` so that testable code can be marked as 
  internal
- To disable standard logging during testing edit `GlobalTestSuiteInitialiser.cs` and pass `LogEventLevel.Fatal`
    to the `LogInitialiser.GetConsoleConfig` initialiser

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

- `centazio host run Centazio.Sample` - This will run the AppSheet / ClickUp integration sample project
- `centazio az func generate Centazio.TestFunctions` - Generate dummy functions (can be many) Azure wrapper 
- `centazio az func generate Centazio.TestFunctions EmptyFunction` - Generate dummy function (single) AWS wrapper
- `centazio aws func deploy Centazio.TestFunctions` - Package and deploy dummy functions to AWS
- `centazio azure func deploy Centazio.TestFunctions` - Package and deploy dummy functions to AWS
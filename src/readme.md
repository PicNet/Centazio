# Todo
- set up testing pipeline (agent is ready)
- careful testing of date handling
- sqlite (self hosted), postgres
- entity creation / update should be closely controlled.  Some fields (like DateCreated) should not be updateable
  and others (like DateUpdated) should not be set when creating.  Use factory methods, etc to control this.  All stores
  should manage these internal fields without requiring the caller to worry about setting things like dateupdated, etc.

- CLI:

- EntityStager:
  - unit tests:
    - check that results from the 'store' are always returned oldest to newest allowing batching 
    - check the limits of the 'stores' will dynamo with current settings handle 1000 records for instance
  - retreiving unpromoted staged entities need to handle:
    - deserialisation to target type
    - filtering:
      - get only the latest for a specific entity
      - centazio 2 even filters out the entity if its the same as previously promoted
  - a mechanism to mark staged entity as ignored

- Promote:
  - support full promote (entire list), i.e. delete all and recreate

- Cloud
  - Use Code Generators to generate Lambda/Azure function wrappers

# Developer Guidelines:

## Code Style 
- 2 tab spaces
- Braces on same line
- Avoid long camme-case names.  Methods should be short so having ugly variableNamesThatAddNoValueInAShortContext should
    be avoided. 
- Class names and method names can be descriptive, allowing for short/concise variables
- Use implicitly typed variables where possible
- Use `is null` / `is not null` instead of `== null` / `!= null`

## Logging
- Logging should prefer lower case.  When describing code (classes, etc) then proper casing is allowed
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
- Exception messages should be lower case.  When describing code (classes, etc) then proper casing as allowed
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
source.  A pattern used in Centazio to handle this is to have the concept of `Raw` objects which are then converted
to their expected types with all required validation.  For instance, when reading the `SystemState` from a staging store, 
since this entity may need to be deserialised, and deserialisation skips most init time validations we need to use the 
following steps:

- Deserialise the entity into a `Raw` dto.  This dto needs to have all fields nullable:
```
public record SystemStateRaw {
  // all fields nullable
  
  public string? System { get; init; }
  public string? Stage { get; init; }
  public bool? Active { get; init; } 
  ...
  
  // validity of fields is done here  
  public static explicit operator SystemState(SystemStateRaw raw) => new(
      raw.System ?? throw new ArgumentNullException(nameof(System)), 
      raw.Stage ?? throw new ArgumentNullException(nameof(Stage)), 
      raw.Active ?? throw new ArgumentNullException(nameof(Active)),
      ...);
}

// usage
var raw = JsonSerializer.Deserialize<SystemStateRaw>(json);
```

- Use the explicit casting operator override to properly validate the deserialised entity:
```
public record SystemState (
    // fields no longer nullable, and correct data types used
    
    SystemName System, 
    LifecycleStage Stage, 
    bool Active,
    ...);
    
var state = (SystemState) raw; // forces validation (explicit operator override)
```
- This `state` object can now be trusted to have valid contents.

This pattern also allows you to apply any required transformations when creating the final entities.  For instance, it
is possible that the source/sink may not support enumeration serialisation, so these could be stored as strings in these
systems and then converted to an `Enum` in the explicit operator override.
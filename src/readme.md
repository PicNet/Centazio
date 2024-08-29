# todo
- set up testing pipeline (agent is ready)
- careful testing of date handling

- CLI:

- EntityStager:
  - each entity type should have its own lifecycle, i.e. keep just one copy, keep 6 months, etc 
  - should make configureable (by entity type) if we check for duplicates using checksum or not.  I.e. do we have
    enough data to have a checkpoint or not.  If not we just use the 'Now' used when calling the API last time

- Promote:
  - support full promote (entire list), i.e. delete all and recreate

# Developer Guidelines:

## Logging
- Logging should prefer lower case.  When describing code (classes, etc) then proper casing is allowed
- Test all structured logging and add any 'clean' versions of structured output to `LogInitialiser.cs` also override
    class `ToString()` method where appropriate to support clean structured logging.
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
- Simple logic should not be unit tested, this includes records, simple properties, etc.
- Special care is needed when testing date related operations.
  - The `TestingUtcDate.Tick()` method can be used to increment the testing `Now` value to ensure date handling
  is being done correctly.
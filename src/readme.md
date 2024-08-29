# todo
- set up testing pipeline (agent is ready)
- careful testing of date handling
- standardise all logging and exception messaging

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

## Exception

## Unit Testing
# Developer Guidelines:

## Code Style 
- 2 tab spaces
- Braces on same line
- Avoid long camel-case names.  Method length should be short, so having ugly variableNamesThatAddNoValueInAShortContext 
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

## NuGet
- Create: `rm packages/*; ./src/bump.sh; dotnet pack -v detailed -c Release -o packages`
- Publish: `dotnet nuget push ./packages/*.nupkg --source https://api.nuget.org/v3/index.json --api-key <api-key>`
- Note: Publishing can take up to 15 minutes to get the new version

## Centazio Cli
- To work with a local version of the Centazio.Cli NuGet tool use:
- `dotnet tool install --prerelease --local --add-source ./nupkg ./packages/Centazio.Cli`
- For rapid development you can use the following approach: 
  - In the directory of the integration project add a reference to the Centazio.Cli project:
  ```
   <ItemGroup>
    <ProjectReference Include="..\path\to\Centazio.Cli\Centazio.Cli.csproj" />
  </ItemGroup>
  ```
  - Create a wrapper for the Centazio.Cli:
  ```
  // Program.cs
  using Centazio.Cli;
  
  Program.Main(args);
  ```
  - Run with `dotnet run --project ./path/to/Wrapper -- arg1 arg2...`

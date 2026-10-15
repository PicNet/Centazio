# Developer Guidelines:

## Development Tools (Prerequisites)
- [dotnet 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [dotnet cli](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [az cli](https://learn.microsoft.com/en-us/cli/azure/?view=azure-cli-latest)
- [az core tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=linux%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp)
- [aws cli](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
- [aws dotnet-lambda cli](https://docs.aws.amazon.com/lambda/latest/dg/csharp-package-cli.html)
- [docker cli](https://www.docker.com/products/cli/)

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
- Unit tests use AWS secrets manager, so running `aws sso login --profile picnet-centazio` (or `centazio dev aws login`) 
    is required on new sessions.
  - Configuration instructions on this profile can be found in internal secrets manager document.
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
- Pushing to GitHub does not publish anything; packages are published from a dev machine
- `centazio dev publish` bumps the patch number in `Directory.Build.props`, packs every package into `packages/` and
    pushes them with `NUGET_API_KEY` from the Centazio secrets.  `-b|--no-bump` publishes the version already in
    `Directory.Build.props` (use it for planned releases such as `0.2.0`), `-p|--no-publish` only packs
- Manual equivalent: `rm packages/*; dotnet pack -c Release -o packages` then
    `dotnet nuget push ./packages/*.nupkg --source https://api.nuget.org/v3/index.json --api-key <api-key>`
- A bump edits `Directory.Build.props` and the `packages.lock.json` files must follow it: run
    `dotnet restore --force-evaluate` and commit both, or CI's locked-mode restore fails
- Note: Publishing can take up to 15 minutes to get the new version

## Centazio Cli
To work with a local version of the Centazio.Cli NuGet tool use:
- `centazio dev publish --no-publish` -- packs into `packages/`; note: the bump it does IS required
- `dotnet new tool-manifest`  -- required only once to set up local dotnet tools in a test dir outside of source tree 
- `dotnet tool install --prerelease --local --add-source ../centazio3/packages/ Centazio.Cli`
- `dotnet centazio [args]` -- run centazio cli
- `dotnet tool uninstall --local Centazio.Cli` -- uninstall

For a more seamless local development copy `src/devcli` into your testing directory and run this each time you want to
rebuild the cli. Consider reverting all the minor version updates before commiting in `Directory.Build.props`.
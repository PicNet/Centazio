# Todo
- aws/azure wrappers:
  - Azure:
    - staging entity providers 
    - fix Log.Logger, currently only injected ILogger<> works 
    - function-to-function triggers
    - AppSheet deploy to Az shows logs 'EmptyFunction running', Empty should not be included
    - `centazio az func simulate Centazio.Sample.ClickUp` fails with error: `System.ArgumentException: Provider Aws is not implemented.`
    - Local simulate `./centazio az func simulate Centazio.Sample.AppSheet`: 
        The listener for function 'Functions.AppSheetPromoteFunction' was unable to start. 
            Microsoft.Azure.WebJobs.Extensions.Timers.Storage: Could not create BlobContainerClient for ScheduleMonitor.
    - Local simulate not showing logs
    - `centazio az func logs` not working - * fixed update func core tool to the latest https://go.microsoft.com/fwlink/?linkid=2174087
      - WT: consider a unit test that checks that core tools is up to date
    - WT: GHA: Unexpected input(s) 'include-prerelease', valid inputs are ['dotnet-version', 'dotnet-quality', 'global-json-file', 'source-url', 'owner', 'config-file', 'cache', 'cache-dependency-path']
  - CP: Aws:
    - function-to-function triggers
    - aws simulate (sam cli local function simulator)
    - generator is including all functions in assembly, even tho we specify a single function
    - centazio aws func deploy throwing docker error
- JB: cli:
  - generators needs a bit of work, comments, etc. Do tutorial
  - dotnet tool install testing
  - good tutorial
  
- GT: secrets needs NUGET_API_KEY.  This is a dev only secret so should not be mandatory
- GT: add `az func logs -w` option to watch for more log changes?
- Centazio func-func trigger test: 
  - all hosts
  - func reads csv with instructions 
  - test confirms only correct triggers received 
  - confirm only required operations run
  - GT: need a simple test to test that func-to-func triggers are working and working efficiently
- GT: need a change-log to be able to do workflows when specific changes to an entity happen. Eg:
  to notify when Entity property Y changes to 'XXX' do something.  Currently we just know the entity changed
  not the property that changed.  This should be part of the promote step.
- GT: a correlation id would be great to track changes to a specific entity through the logs, it would be good to
    store jsons of the object at each stage to be able to reproduce any transformations through the pipeline
- GT: is it possible to use EntityChange as a function trigger? 
- GT: a good integration test that checks:
  - package nuget
  - dotnet tool install (from local nuget)
  - in a non-dev directory
  - centazio gen sln
  - centazio gen func
  - create simple func (that causes a side effect that can be checked)
  - run func using `centazio host`
  - test func worked
  - test func in azure that works
  - test func in aws that works
- GT: settings objects should have extension methods for helpers such as AwsSettings.GetRegionAsEndpoint to
    replace duplicate code throughout such as AwsSecretsLoader.cs
- GT: the readme/tutorial should automatically insert code samples from the real codebase, instead of duplicating it
- GT: a good simulation tester, using excel to simulate data flows perhaps?
- GT: create a good set of architectural policies that can be validated using NetArchTest, 
    see: https://dateo-software.de/blog/netarchtest (replace tests in `Centazio.Core.Tests.Inspect`)

- snowflake provider
- better cloud settings management (env vars?) - including cron timers
- remove all ugly usage of auto Dto conversions, remove DtoHelpers, use manual mapping only (settings already done)
 

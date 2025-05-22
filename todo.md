# Todo
- WT: better secrets management
- WT: does CLI aws command work with new secrets manager?
- CP/WT: aws/azure wrappers:
  - WT: Azure:
    - fix Log.Logger, currently only injected ILogger<> works
    - application insights (is this completed?) 
    - function-to-function triggers
  - CP: Aws:
    - function-to-function triggers
    - aws simulate (sam cli local function simulator)

- JB: cli:
  - generators needs a bit of work, comments, etc. Do tutorial
  - dotnet tool install testing
  - good tutorial
  
- GT: secrets needs NUGET_API_KEY.  This is a dev only secret so should not be mandatory
- Centazio func-func trigger test: 
  - all hosts
  - func reads csv with instructions 
  - test confirms only correct triggers received 
  - confirm only required operations run
- GT: need a change-log to be able to do workflows when specific changes to an entity happen. Eg:
  to notify when Entity property Y changes to 'XXX' do something.  Currently we just know the entity changed
  not the property that changed.  This should be part of the promote step.
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
 

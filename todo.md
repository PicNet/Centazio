# Todo
- a good integration test that checks:
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
- aws/azure wrappers:
  - Azure:
    - fix Log.Logger, currently only injected ILogger<> works
      - this will somewhat resolve the issue: var config = LogInitialiser.GetFileConfig(dir: @"C:\home\LogFiles\Application\Functions\Function\EmptyFunction");
      - however, we now get duplicate logs and they are a delayed
    - application insights
    - key vault, see: https://claude.ai/chat/2538ccd6-d0e3-49a2-a155-74972ddb606b
    - currently settings are done using the `settings.json` files, these are hard to change in Azure, so perhaps find 
        a way of converting to standard .Net settings so they can be set using the Azure UI.  If this is done then
        the CRON triggers should be in settings instead of hardcoded in the generated function wrappers
    - support overriding function specific settings like service plan, etc
    - function-to-function triggers
  - Aws:
    - https://docs.aws.amazon.com/lambda/latest/dg/csharp-image.html
    - image: public.ecr.aws/lambda/dotnet:9
    - currently hardcodes 1 minute trigger
    - function-to-function triggers

- cli:
  - simulate should not open a new window when running just one function
  - generator needs a bit of work, comments, etc.  Do tutorial

- new `Centazio.TestFunctions` that implement read/promote/write so we can fully test all workflows
- the readme should automatically insert code samples from the real codebase, instead of duplicating it

## Low
- snowflake provider
- remove all ugly usage of auto Dto conversions, remove DtoHelpers, use manual mapping only (settings already done)
- create a good set of architectural policies that can be validated using NetArchTest, see: https://dateo-software.de/blog/netarchtest
  - many tests in `Centazio.Core.Tests.Inspect` namespace can be improved using NetArchTest

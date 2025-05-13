# Todo
- secrets management 
  - replace current SecretsLoader with Azure Key Vault loader
    - local development
    - devops pipelines
    - prod workloads
- aws/azure wrappers:
  - Azure:
    - fix Log.Logger, currently only injected ILogger<> works
    - application insights (is this completed?) 
    - function-to-function triggers
  - Aws:
    - AwsSecretsLoader and corresponding IServiceFactory 
    - https://docs.aws.amazon.com/lambda/latest/dg/csharp-image.html
    - image: public.ecr.aws/lambda/dotnet:9
    - currently hardcodes 1 minute trigger
    - function-to-function triggers

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
- cli:
  - simulate should not open a new window when running just one function
  - generator needs a bit of work, comments, etc.  Do tutorial

- new `Centazio.TestFunctions` that implement read/promote/write so we can fully test all workflows
- the readme should automatically insert code samples from the real codebase, instead of duplicating it
- `centazio host run` on generated folder not working (see test branch dir=sample2/CentazioTestin cmd=centazio host run CentazioTesting.Shared)

## Low
- snowflake provider
- a good simulation tester, using excel to simulate data flows perhaps?
- remove all ugly usage of auto Dto conversions, remove DtoHelpers, use manual mapping only (settings already done)
- create a good set of architectural policies that can be validated using NetArchTest, see: https://dateo-software.de/blog/netarchtest
  - many tests in `Centazio.Core.Tests.Inspect` namespace can be improved using NetArchTest
- settings.json is uploaded to aws/azure, should these be converted to app settings env variables
- key vault/secrets manager for secrets instead of uploading secrets file (or use app settings env variables)
    - app settings env variables can be set here: AzFunctionDeployer#CreateFunctionAppConfiguration
    - cron triggers are hardcoded, using env variables this could then be changed to a setting

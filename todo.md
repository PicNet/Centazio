# Todo
- ci (github-hosted runners):
  - HIGH: generated Az function projects fail to restore (NU1608). `AddLatestNuGetReferencesToProject` takes the latest 
    of every package, and latest `Microsoft.ApplicationInsights.WorkerService` (3.x) conflicts with latest 
    `Serilog.Sinks.ApplicationInsights` (needs ApplicationInsights < 3). Breaks `Test_Az_GenerateSolution`, 
    `Test_CreateFunctionAppZip`, `Test_DotNetCliProjectBuilder_BuildProject` and the cli itself
  - MED: Azure test resources are gone: Key Vault `CentazioDevKeyVault` no longer resolves and the `AZ_CLIENT_ID` app in 
    the `picnet/centazio/dev` secret is not in the tenant. Az tests are skipped via `AzTestingEnv.IgnoreIfVaultUnreachable`
    until recreated; then add `AZURE_CLIENT_ID`/`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID` repo variables (OIDC) to 
    enable the `azure login` workflow step
  - MED: remove `continue-on-error: true` from the `dotnet test` step once the build is clean, failing tests currently 
    still report a green run
  - LOW: LocalStack is pinned to 4.14.0, the last release that runs without a licence
  - LOW: runner is pinned to `ubuntu-24.04`, Microsoft does not yet publish `azure-functions-core-tools-4` for 26.04 
    (`ubuntu-latest`)
  - LOW: deregister the `dev-build-ubuntu` self-hosted runner once hosted runs are proven
- security (ci review 2026-10-21, needs account owner):
  - HIGH: CI role `github-actions-centazio` can read all of `picnet/centazio/dev`, including `NUGET_API_KEY` and a 
    long-lived key for AWS account 780133770227. Move the values tests need into a CI-only secret, limit the role to it, 
    and replace the NuGet key with one scoped to the Centazio packages
  - MED: any branch of the repo can assume the CI role (6 people with write access). Consider trusting only `master`, or
    a protected environment with reviewers, and running branch builds without secrets
  - MED: name the exact test secrets in the role policy instead of `testing*` etc, and replace the `picnet/centazio/*` 
    read wildcard so a future `picnet/centazio/prod` is not readable
- aws/azure:
  - MED: better cloud settings management (env vars?) - including cron timers
  - Azure:
    - MED: function-to-function triggers
    - HIGH: 
      - `./centazio az func logs Centazio.Sample.AppSheet` occurs `Unable to connect to Azure. Make sure you have the 'az' CLI or 'Az.Accounts' PowerShell module installed and logged in and try again`
        - Need to login like `az login --scope "https://management.core.windows.net//.default"`          
      - Centazio.Sample.AppSheet deploy to Az shows logs 'EmptyFunction running', Empty should not be included
      - You can check logs with `./centazio az func logs Centazio.Sample.AppSheet`    
  - Aws: 
    - HIGH: generator is including all functions in assembly, even though we specify a single function
    - MED: aws simulate (sam cli local function simulator)
    - LOW: function-to-function triggers - currently for aws, we are triggering functions once per 'trigger' object.  We are not merging them into a list
    - LOW: implement `./centazio aws func logs ...`

- LOW: cli:
  - generators needs a bit of work
  - dotnet tool install testing
  - good tutorial

- Remove dotnet tool
- Remove sln/proj/func code generator.  Leave AWS/Azure function wrappers

GT: 
- E2E Core entities and system entities should have strong value type ids, such as CrmMembershipTypeId.  This will test
  that subclassing CoreEntityId/SystemEntityId works as expected
- Centazio func-func trigger test: 
  - all hosts
  - func reads csv with instructions 
  - test confirms only correct triggers received 
  - confirm only required operations run
  - need a simple test to test that func-to-func triggers are working and working efficiently
- is it possible to use EntityChange as a function trigger on specific property change? 
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
- the readme/tutorial should automatically insert code samples from the real codebase, instead of duplicating it
- a good simulation tester, using excel to simulate data flows perhaps?
- create a good set of architectural policies that can be validated using NetArchTest, 
    see: https://dateo-software.de/blog/netarchtest (replace tests in `Centazio.Core.Tests.Inspect`)

- LOW: additional providers:
  - azure blob storage staged entity store
  - azure cosmos db staged entity store
  - snowflake providers

 

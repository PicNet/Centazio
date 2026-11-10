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
- security (ci review 2026-10-21):
  - MED: any branch of the repo can assume the CI role `github-actions-centazio` (6 people with write access), which 
    reads `picnet/centazio/dev` (ClickUp, AppSheet, SQL connection string). Consider trusting only `master`, or a 
    protected environment with reviewers, and running branch builds without secrets
  - LOW: `AWS_KEY`/`AWS_SECRET` in `picnet/centazio/dev` belong to account 780133770227, which has no IAM users or root 
    keys, so they are dead. The cli AWS commands (`AbstractAwsCommunicator`, `AwsFunctionDeployer`) use them, so AWS 
    deploys from the cli need a working credential (preferably the SSO profile) before the keys and the assertion in 
    `SampleSecretsLoaderTests` are removed
- core (found in review 2026-11-11, verify each before fixing):
  - HIGH: `WriteFunction` persists nothing before calling the target api and an exception aborts the whole operation 
    (`AbstractFunction.RunOp`, "partial success not addressed"). A failure mid-batch, or a crash between the api call 
    and `CreateSysMap`, re-creates already written entities on the next run (duplicate invoices/payments). Needs 
    pending maps saved before the write and/or idempotency keys, plus per-entity write results 
    (Success/Error/MissingTarget/Ignore) with controlled retry of errored entities. `PendingUpdate.Error(...)` exists 
    but is never called. Drive the design from the simulation crash-point tests below
  - MED: `SelfHost.RegisterHostSpecificServices` creates `InProcessChangesNotifier` with `using var`, which may dispose 
    (complete the channel of) the notifier as soon as registration returns
  - MED: `FunctionConfigDefaults.DefaultFirstTimeCheckpoint` is evaluated once at type init, so it does not follow the 
    current (or test) clock
  - MED: `tests/Centazio.Sample.Tests` calls the real ClickUp/AppSheet apis (and deletes real tasks). Move to simulated 
    systems, keep a small opt-in contract test against the real apis
  - LOW: sample api clients (`ClickUpApi`, `AppSheetApi`) use a `private static HttpClient` and never check the response 
    status code
  - LOW: no retry/backoff/rate limit support anywhere. Decide what belongs in the framework (e.g. a standard resilient 
    `HttpClient` pipeline with Retry-After support that never retries non-idempotent requests after a response was 
    received) vs. in each api client
- aws/azure:
  - MED: better cloud settings management (env vars?) - including cron timers, per environment (cyca needs e.g. 
    daytime-only crons in uat)
  - Azure:
    - HIGH: function-to-function triggers. `AzHost` uses a plain `FunctionRunner` with no `IChangesNotifier`, so Az 
      functions are timer only. Required for the cyca migration (currently chains functions with Service Bus topics). 
      Likely an Az Service Bus (or Storage Queue) `IChangesNotifier` + generated trigger functions
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

- MED: cli (keep the dotnet tool and the sln/proj/func generators, fix them rather than remove them):
  - generators needs a bit of work (see the NU1608 and aws 'all functions' HIGHs above)
  - dotnet tool install testing (see the GT integration test below)
  - good tutorial

- HIGH: simulation testing framework. Goal: unit tests that simulate real environments (api latency, failures, rate 
  limits, crashes, multiple systems syncing over days of simulated time), deterministic and replayable from a seed. 
  Grow `Centazio.Test.Lib/E2E` (CRM/Fin epochs, `RandomSimulation`, `EpochTracker`) rather than start again. Breaking 
  changes are fine. Phases:
  - 1. determinism seams (prerequisite):
    - replace static `UtcDate`/`TestingUtcDate` with an injected `TimeProvider` (`FakeTimeProvider` in tests), per 
      engine, not process wide. Update the `DateTimeUsageTests` inspection accordingly
    - remove wall clock waits from the run path: `AsyncUtils.ConditionalDelay`, `Task.Run`/`Task.Delay(1)` in 
      `FunctionRunnerWithNotificationAdapter`, `InProcessChangesNotifier` background loop, `Synchronous(throttle)`
    - one seeded random source per simulation (replace static `Rng` and the unseeded `Random` in 
      `GlobalEnumerableExtensionMethods`), deterministic id generation
    - in-memory provider factory so a real `CentazioEngine` (DI, integrations, settings) can be built in tests
    - remove other statics that leak between tests (`FunctionConfigDefaults.ThrowExceptions`, `AzHost.impl`, ...)
  - 2. simulated time host: evaluates function crons against the simulated clock, runs due functions, dispatches 
    func-to-func triggers, runs until quiescent. `RunFor(TimeSpan.FromDays(3))` should take milliseconds. Must support 
    overlapping runs (timer + trigger at once, long running function) to test the running/stuck guards
  - 3. simulated systems kit: reusable `SimulatedSystem` (in-memory state + endpoints + business rules + a log of every 
    mutation received) served through an `HttpMessageHandler`, so the real api client code (paging, auth, rate limit 
    handling) is under test. Also a lighter interface level fake for pure function logic tests. Contract tests 
    (opt-in, against sandboxes) to stop fakes drifting from the real apis
  - 4. fault library, configurable by rate, time window or rule, latency advancing the simulated clock:
    - latency distributions, timeouts, connection resets, 5xx
    - 429 with `Retry-After` and rate limit usage headers
    - applied server side but response lost (the duplicate write case)
    - eventual consistency (written entity not readable for N seconds)
    - data changing while paging, out of order updated timestamps, clock skew between systems
    - malformed and very large responses
  - 5. crash points: named hooks in the framework (e.g. after api write, before map save; mid batch; during promote 
    transaction) where the simulator kills the run, then restarts it. Drives the `WriteFunction` HIGH in core above
  - 6. invariant based validation, checked after quiescence: systems converge per mapping rules, no external side 
    effect applied twice, no lost updates, checkpoints monotonic, nothing left `Running`, every error recorded. 
    Randomised runs over many seeds x fault profiles; on failure print the seed and a timeline so it replays exactly
  - 7. scenario DSL for targeted tests, e.g. 
    `sim.At(t0).In(Crm).Create(customer); sim.Between(t1, t2).In(Fin).Fault(429, rate: 0.3); sim.RunFor(3.Days())`. 
    Optional: scenarios as csv/excel tables
  - 8. port the existing E2E CRM/Fin tests onto the framework, including the func-to-func trigger test:
    - all hosts
    - func reads csv with instructions 
    - test confirms only correct triggers received 
    - confirm only required operations run
    - simple test that func-to-func triggers are working and working efficiently
  - 9. use it for the cyca migration (below), cyca production bugs become regression scenarios

- HIGH: migrate `../cyca-piomp` to this version of Centazio. Currently runs on an old vendored copy of Centazio 
  (`infra/centazio*`: sync EF Core over SQL Server, db-first `[Infra]` schema, Service Bus chaining via codegen'd 
  triggers, 6 Az function apps, 16 function classes, 28 triggers). Full redesign of the core model to fit the new 
  Centazio methodology is accepted. Fixes to current production are done in the cyca-piomp repo, not here, but known 
  bugs must not be carried into the new version.
  - framework prerequisites (here):
    - Az function-to-function triggers (see Azure HIGH)
    - `WriteFunction` pending state / idempotency / per-entity results (see core HIGH)
    - first class 'Other' functions (reports, monitoring, scheduled jobs that are not read/promote/write): HC audit 
      report, infra error monitoring and daily report, NewBook occupancy report, Xero balances / P&L
    - `ShouldRun` style gating per function/operation (Xero business hours debounce)
    - notifications: error alerting and summary emails, must still work when the db is down
    - per environment crons and settings (dev/demo/uat/preprod/prod)
    - staged entity archival (`DeleteStagedBefore`/`DeletePromotedBefore`) on a schedule
    - verify stale run recovery replaces the old db overlap guard (`IsStillRunning(630s)`)
  - design decisions:
    - redesign `systems.data` core entities onto `CoreEntityBase` (checksums, `CoreStorageMeta`), replacing int ids/
      `source_system`/`source_id`, relational FKs via `GetRelatedSystemIdsFromCores`/`GetRelatedCoreIdsFromSystemIds`
    - the very wide `Member` table (HC and NB specific columns) should be split per the new model
    - one-shot actions currently tracked with fake target PKs (`"{id}:{now}"`: markPaid, NB `CreatePayment`, credit 
      notes) need a proper design
    - old writes only from the creating system, new excludes by `LastUpdateSystem`: confirm the intended semantics per 
      entity
    - old stager keeps latest per `SourceId` and drops items identical to anything promoted in the last day, new dedupes 
      by checksum only: confirm sufficient
    - hosting: keep the SQL 2019 VM or move to Azure SQL; one function app per system or fewer
    - replace the record/replay http cache tests (`CacheableHttpClient`) with simulated systems + opt-in contract tests
  - drop (dead or legacy): Bepoz paths and the `BepozPromote` hack, Shopify stock write (Bepoz stock), vouchers, 
    Hangfire host (`infra/centazio.host`), stale `systems/end2end.test`, `web`/`centazio.websrv` admin ui (not deployed, 
    revisit later), `systems.tasks` ad-hoc maintenance scripts (replace with proper cli commands if still needed)
  - do not carry over:
    - retries that re-execute non-idempotent requests (Lightspeed still re-sends POSTs when rate limit usage > 80%, same 
      bug as `516ffc2c` in HC; integer division `ratelimitused / ratelimit`)
    - unbounded recursive 429 retry (HC), missing 429 handling (NewBook, Xero), NB read 'checkpoint - 5 minutes' hack
    - fragile `.Single` result matching on Xero batch writes (batch results may be out of order)
    - committed secrets: `local.settings.json` with the Service Bus connection string, HC test password in a comment in 
      `HelloClubApi.cs`, SQL VM public IPs in the readme
    - misspelled Lightspeed config key (`..._millisec` vs `..._millisecs`)
  - systems to port (flow: HelloClub is member master -> NewBook, Xero, Lightspeed; HC and NB invoices -> Xero for 
    current HC members only; Xero payments/credit notes/overpayments -> HC and NB; no payments written to Xero):
    - HelloClub: memberships, members, transactions, payments (read/promote/write, audit report, new member emails)
    - NewBook: guests, invoices, receipts, payments, occupancy report
    - Xero: contacts, contact groups, invoices (+ emailing rules, Feb blackout), payments, credit notes, overpayments, 
      reports
    - Lightspeed: customers (write only)
    - Shopify: orders, products (read/promote only, confirm still required)
    - Infra: error digest, archival, daily report
  - simulation regression scenarios (from cyca production bugs):
    - duplicate HC transactions / multiple payments from a re-executed POST (`516ffc2c`)
    - crash between api write and map save
    - external invoices attaching to existing YSA donation transactions, idempotency suffix collisions (`cee550ed`, 
      `554d1f8d`, `a12358fb`)
    - invoice already paid in HC so total 0 and payment fails (`8ee28c18`), partial payment totals (`f1408fd5`), double 
      or over payment (`c5eb4de6`)
    - NB invoices with null/unknown `guest_id` (`8adca82d`, `62701c04`), NB GL lookup returns nothing (`33b555b9`)
    - Xero invoice paid before its map is saved, duplicate invoice number (`XeroPromote.FilterInvoices`)
    - echo suppression: entities written by Centazio read back from the target ('Transaction From:', bounce back)
    - non-members not synced (`a801d885`), large responses (`f52f8230`, `136cd660`)
  - cutover:
    - convert `[Infra].TargetSystemEntity` -> `CoreToSysMap` and `SystemStateEntity.IncrementalRefreshDate` -> 
      `ObjectState` checkpoints, plus core data -> new core model. Mapping history is mandatory to avoid duplicate writes
    - run new read/promote in shadow in uat (writes disabled), compare core data with the old system
    - switch writes per target system, disabling the old writer for that target as each moves
    - infra: bicep, NSG/outbound ip rules, Service Bus, App Insights, settings backup/restore scripts

GT: 
- E2E Core entities and system entities should have strong value type ids, such as CrmMembershipTypeId.  This will test
  that subclassing CoreEntityId/SystemEntityId works as expected (do as part of simulation phase 8)
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
- create a good set of architectural policies that can be validated using NetArchTest, 
    see: https://dateo-software.de/blog/netarchtest (replace tests in `Centazio.Core.Tests.Inspect`)

- LOW: additional providers:
  - azure blob storage staged entity store
  - azure cosmos db staged entity store
  - snowflake providers

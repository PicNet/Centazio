# Todo
- aws/azure:
  - HIGH: `./centazio az func simulate Centazio.Sample.ClickUp` error: `AmazonClientException: Failed to resolve AWS credentials. The credential providers used to search for credentials returned the following errors`
  - MED: better cloud settings management (env vars?) - including cron timers
  - Azure:
    - MED: function-to-function triggers
    - HIGH: 
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

 

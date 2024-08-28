# todo
- set up testing pipeline (agent is ready)

- CLI:

- EntityStager:
  - each entity type should have its own lifecycle, i.e. keep just one copy, keep 6 months, etc 
  - should make configureable (by entity type) if we check for duplicates using checksum or not.  I.e. do we have
    enough data to have a checkpoint or not.  If not we just use the 'Now' used when calling the API last time
- need to save laststart/completed, etc in SystemState

- Promote:
  - support full promote (entire list), i.e. delete all and recreate
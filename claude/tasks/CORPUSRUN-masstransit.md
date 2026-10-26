# Corpus run MassTransit (TAAK-310 step 4)

Scan round 1 (L4, 2026-10-26): `unshackle scan --fix masstransit --max-degree 4 --report` with the tool from `pool/l4-epplus-corpus-wip` 7a27c9e
on the 24 repos of `claude/tools/corpus-masstransit-list.txt` (survey `docs/projectinfo/architectuur/corpus-masstransit.md`). The fix is
report-only, so "Info" counts the inventory/guidance findings and "Manual" the detector findings; no code is written, so there is no after round.
Harness: `claude/tools/corpus-epplus-scan.py` with `CORPUS_SRC=.` (repos sit directly under the corpus root).

## Result

19 of 24 repos scanned; every detector fired somewhere; no pattern crashed. The column "Build / scan" holds exit codes and seconds.

| Repo | Build / scan | Info | Manual | Crash in log |
| --- | --- | ---: | ---: | --- |
| Altinn_altinn-auth | build=0 86s scan=0 73s | 1 | 3 |  |
| AntonioFalcaoJr_Reminders | build=0 13s scan=0 6s | 2 | 3 |  |
| ardalis_VerticalCleanModularMicroservices | build=0 24s scan=0 16s | 1 | 0 |  |
| baranacikgoz_modular-monolith-ddd-vsa-webapi | build=2147516571 0s scan=1 1s | 0 | 0 | yes |
| EdwinVW_cloud-native-net | build=0 15s scan=1 0s | 0 | 0 | yes |
| elsa-workflows_elsa-extensions | build=0 180s scan=0 97s | 18 | 19 |  |
| evolutionary-architecture_evolutionary-architecture-by-example | build=1 21s scan=0 15s | 1 | 0 |  |
| firebend_auto-crud | build=2147516571 0s scan=1 0s | 0 | 0 | yes |
| firebend_jobba | build=2147516571 0s scan=1 0s | 0 | 0 | yes |
| gncyyldz_Saga-Orchestration | build=0 10s scan=0 8s | 9 | 10 |  |
| iblazhko_eventsourced-processmanager | build=0 17s scan=0 13s | 11 | 5 |  |
| iongritco_ModularMonolith | build=0 10s scan=0 13s | 4 | 2 |  |
| jokk-itu_MassTransit-Outbox-Sample | build=0 8s scan=0 3s | 5 | 7 |  |
| Letterbook_Letterbook | build=0 50s scan=0 36s | 30 | 22 |  |
| marinasundstrom_YourBrand | build=0 203s scan=0 128s | 25 | 218 |  |
| NikiforovAll_social-media-app-aspire | build=1 18s scan=1 11s | 0 | 0 |  |
| nkz-soft_microservice-template | build=0 42s scan=0 24s | 4 | 0 |  |
| NoxOrg_Nox | build=0 65s scan=0 59s | 2 | 8 |  |
| onebeyond_monaco | build=0 22s scan=0 21s | 4 | 16 |  |
| sloweyyy_cloud-native-ecommerce-platform | build=0 21s scan=0 30s | 12 | 10 |  |
| SwissLife-OSS_Confix-Unloaded | build=2147516571 0s scan=1 1s | 0 | 0 | yes |
| thangchung_coffeeshop-on-nomad | build=0 18s scan=0 14s | 5 | 22 |  |
| thecodewrapper_CH.CleanArchitectureBlazor | build=0 26s scan=0 27s | 3 | 7 |  |
| xilapa_SiteWatcher | build=1 15s scan=0 14s | 2 | 10 |  |

| Pattern | Status | Count |
| --- | --- | ---: |
| masstransit.consumer | Manual | 115 |
| masstransit.publish-send | Info | 107 |
| masstransit.request-client | Manual | 72 |
| masstransit.transport | Manual | 66 |
| masstransit.registration | Manual | 54 |
| inventory-only | Info | 18 |
| masstransit.outbox | Manual | 18 |
| masstransit.test-harness | Manual | 17 |
| masstransit.consumer | Info | 14 |
| masstransit.consumer-definition | Manual | 10 |
| masstransit.retry | Manual | 9 |
| masstransit.saga | Manual | 1 |

| Pattern | Manual code | Count |
| --- | --- | ---: |
| masstransit.consumer | ConsumerFilter | 98 |
| masstransit.request-client | RequestResponse | 72 |
| masstransit.transport | Transport | 66 |
| masstransit.registration | BusRegistration | 54 |
| masstransit.outbox | Outbox | 18 |
| masstransit.test-harness | TestHarness | 17 |
| masstransit.consumer-definition | ConsumerDefinition | 10 |
| masstransit.consumer | ConsumerWithDefinition | 10 |
| masstransit.retry | RetryPolicy | 9 |
| masstransit.consumer | ConsumerPublish | 4 |
| masstransit.consumer | ConsumerContextFeature | 2 |
| masstransit.saga | Saga | 1 |
| masstransit.consumer | ConsumerRespond | 1 |

## Failures

- baranacikgoz, EdwinVW, firebend_auto-crud, firebend_jobba, SwissLife-OSS: global.json asks an SDK that is not installed (10.0.402, 6.0.0, 7.0.203, ...).
  The scan then ends in an **unhandled** `RemoteInvocationException` (hostfxr_resolve_sdk2) instead of a clean "solution could not be loaded"
  message: a bug in the DataAccess solution loader (outside lane L4; same in EPPlus repo lesi, `CORPUSRUN-epplus.md`).
- NikiforovAll: the deprecated Aspire workload (NETSDK1228) and a second package source from the machine config with central package
  management: environment, reported cleanly.

## Open

- Pin the repo shas (shallow clones of HEAD on 2026-10-22).
- A second round after the loader fix, with SDK pins rolled forward (as `corpus-regression.py globaljson` does on Linux).

## See also

- `claude/tasks/TAAK-310-masstransit-v8.md` — the task
- `claude/tasks/CORPUSRUN-epplus.md` — the EPPlus round with the same harness

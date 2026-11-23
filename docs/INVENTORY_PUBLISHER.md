# Disabled inventory snapshot receiver

This is a review-only implementation, not a deployed/activated connector. The
checked-in EnableInventoryIntegration flag is false, no publishers or keys exist,
and reservations remain disabled. No Supabase products/policies are touched.

POST /api/v1/integration/inventory-snapshots requires HTTPS and a separate
X-Inventory-Publisher-Key (at least 32 characters). There is no anonymous write
fallback: AllowAnonymous bypasses only user JWT so the mandatory dedicated publisher
credential can be used instead. Publisher configuration stores SHA-256 key hashes,
not raw keys. Provision high-entropy keys outside source control, one per branch.
Duplicate hash grants fail closed. Revoke by removing its grant. Never pass keys in
query strings or public browser JavaScript. Request logs do not include headers/body.

After explicit setup, use secret configuration InventoryIntegration:Publishers with
PharmacyId, BranchId, InstallationId, KeySha256. This PR deliberately includes no
working grant, activation UI, issuance API or real pharmacy data. Before activation,
confirm branch map pin/address, HTTPS deployment, reviewed mappings, dependency
security warnings and database/provider concurrency tests.

Schema v1 envelope uses camelCase: schemaVersion=1, installationId, branchId,
snapshotId, sequence, observedAtUtc (UTC, <=15 minutes old, <=2 minutes future),
mode=partial, items. Each item has externalProductId, medicineId,
identityFingerprint (64 hex chars), availableQuantity (whole non-expired boxes),
price (positive EGP, at most 2 decimals), currency=EGP, saleUnit=box, unitsPerPack.
The app-side preview currently emits decimal strings, so the receiver uses the
ASP.NET web JSON defaults that permit reading numeric strings. Integration must
keep this interoperability covered by tests.

An operator must create InventoryPublisherBindings only after exact GTIN/pack
review, including branch/installation,medicine ID,identity fingerprint and pack
unit count. There is no public binding-write endpoint. Missing/changed/unverified
bindings reject the entire batch. No guessed catalog insertion. No partial writes:
one serializable transaction saves inventory plus receipt. Same snapshot ID and
payload returns its receipt;different payload conflicts. Sequence increases per
branch across installations;one active writer per branch. Concurrent conflicts
return409 and must be retried with the same snapshot. No receipt retention policy
is implemented yet: do not prune sequence history without a durable high-water mark.

Zero stock is an explicit update, not omission. Missing rows are not zeroed.
Inactive/deleted mappings require a later explicit retirement/tombstone workflow.
Current receiver supports partial updates only. Prices are per reviewed whole box.
Sync cannot run while reservations are enabled or while a row has reserved stock.
Keep manual stock edits off for connector-owned branches. UpdatedAt preserves source
observation time; retries and delayed queues do not manufacture fresh availability.
Older observations cannot overwrite a newer successful sync for that row.

Public finder uses only platform branch inventories,not Supabase catalog fields.
Older-than15-minute/future-dated observations show Unknown (not OutOfStock),with
quantity suppressed. Fresh explicit zero shows OutOfStock. No matching data says
availability unknown. Branch contacts remain so users can call. Synced prices are
labelled per pack;legacy units remain unspecified,not relabelled as packs. This
requires deployment tests and a reviewed stale-offer policy before live use.

The Inventory receiver safety workflow runs seven focused real SQL Server LocalDB
checks on an isolated Windows runner database. It covers migration application,
persistent replay receipts, changed payloads, reorder/old observations, concurrent
writers, all-or-nothing rollback, branch/installation isolation and public freshness.
The suite never reads the app connection string. A failed or unavailable workflow
is still a merge blocker; a workflow definition is not evidence of a passing run.
Branch writers lock their branch row before examining receipts to avoid a first
snapshot key-range conversion deadlock. Other SQL deadlocks/lock timeouts return a
retryable 409 and never acknowledge uncommitted work.

Tests/build: dotnet test tests/PharmacyAvailability.UnitTests; npm test and npm run
build under src/PharmacyAvailability.Web. SQL Server integration suites currently
require Windows LocalDB and cannot be claimed passing on Linux. Before enabling,
run actual SQL Server retry/reorder/concurrent transaction,rollback,branch isolation,
receipt persistence and migration tests. The preview/receiver is not an end-to-end
connector until the desktop durable queue/settings PR exists.

Fresh-database SQL testing found an existing AddBranchToInventory migration bug:
its raw SQL referenced BranchId before adding it. This PR now creates the column
first. Legacy inventory migrates automatically only when its pharmacy has exactly
one branch. Missing/ambiguous branches cause an explicit transactional failure,
not the old deletion of stock and history. Existing databases that already applied
that migration are not rewritten. Back up and review legacy branch assignments
before any migration; no live database was touched here.

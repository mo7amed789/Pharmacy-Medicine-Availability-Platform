# Synthetic cross-project trial

No deployed site, real pharmacy, real stock or reusable credential is used.
Production feature flags and publisher grants remain unchanged.

The workflow checks out the exact merged desktop app8177e956a15c7fa78cef17cd40cf593f562c9abb.
It starts isolated Windows SQL Server LocalDB and a fake sales SQLite database.
The actual desktop mapping review, durable outbox and acknowledgement code runs
in Python. A subprocess bridge hands its request to the real ASP.NET HTTP pipeline
(WebApplicationFactory), which writes SQL Server. The public finder HTTP endpoint
is queried after each accepted request, including nearest-distance filtering.

The trial exercises:
- Exact synthetic GTIN review,45nonexpired units/20units per box gives2boxes.
- Temporary delivery failure, closing/reopening sidecar, same snapshot retry.
- Credential required, reviewed server binding, accepted acknowledgement.
- Identical snapshot replay without duplicate receipt or changed stock.
- Synthetic sellout sends explicit zero and finder says OutOfStock.
- Sale unit remains box and fake branch distance is zero at its fake coordinates.

Limits: the transport seam is bridged in-process, not a real internet/TLS socket.
No public hosting, certificates, DNS, firewall, physical Windows billing device or
user stock is tested. It does not prove a production deployment or eliminate the
500reviewed-binding and explicit-retirement limits. Known package audit status is
a separate check, not proof that the whole app is free of security issues.

## One real pilot still needs

1. The intended pharmacy and branch name, public address, confirmed map pin,
   public phone and opening hours. Never infer a pharmacy from a home location.
2. Hosting choice and approved budget, or permission to propose hosting options.
   A public HTTPS URL and supported SQL Server database need separate approval.
3. Approval to provision one high-entropy branch publisher credential securely,
   not in chat, git or public JavaScript. Server stores only its SHA256 hash.
4. Operator review of a small medicine/box mapping set on both systems, permission
   for that limited real stock pilot, and a rollback/revocation plan.
5. Backup/migration review, coordinated sidecar restore policy, native Windows
   billing/UI check and real HTTPS end-to-end acceptance before public enablement.

No item in this test file grants deployment, spending or stock-upload permission.

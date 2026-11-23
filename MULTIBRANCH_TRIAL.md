# Multi-branch isolated trial

This trial uses three invented branches, invented stock and synthetic credentials. Nothing is deployed or enabled. Public CI generates501 invented product names and check-digit-valid test-only GTIN strings. These identifiers are not claims of GS1 assignment. No exported catalog is committed.

For a private local exported catalog copy, set CATALOG_TRIAL_FIXTURE to a JSON array of501 records containing trade_en, trade_ar, strength, form, barcode and sell_price. Names/barcodes/prices stay unchanged. Each needs a unique strict-valid GTIN and a positive two-decimal price. The trial's pack size of1 is an explicitly synthetic assumption, not live pack verification.

Each branch tests501 exact candidates (all require review), unmatched and ambiguous candidates,500 published bindings, explicit zero stock, and501 bindings blocking both a new snapshot and a queued500-row snapshot. The receiver test additionally checks1500 accepted rows across branches and the public nearest-branch order.

The Python local driver can run without SQL using a subprocess acknowledgement harness. It does not prove the receiver or public finder. The combined CI test uses SQL Server LocalDB and the real ASP.NET middleware/controller through an in-process HTTP test host. It does not test public TLS/sockets or a physical desktop device.

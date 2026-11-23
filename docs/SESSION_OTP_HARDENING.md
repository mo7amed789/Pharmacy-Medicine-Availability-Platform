# Session and verification-code hardening

No production changes are applied by this PR.

- Refresh tokens are kept in page memory only, never browser storage. Existing persisted copies are removed at startup. A page reload or closing the tab requires sign-in. This reduces persistent token theft, but does not protect a running page from XSS. A future same-site HttpOnly cookie design should include CSRF controls.
- Verification codes use per-code random salts and PBKDF2-SHA256, 210,000 iterations, fixed-time comparison, and the existing five-attempt limit and expiry. New values are never plaintext.
- Apply the HashVerificationCodes database migration before running the updated API. It expands Code to128characters and deletes pending codes so old plaintext values do not remain. Users must request new codes. Rollback deletes pending hashes before shrinking the column; codes are not recoverable. Do not run a rolling mixed-version deployment.
- No live keys, policies or deployments were changed. No tokens were generated against a live service.

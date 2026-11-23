# Staff branch restrictions

Deploy database migration AssignStaffBranches BEFORE the updated API, then deploy the matching web app. This is a breaking authorization change for staff, not admins. Do not deploy while staff need uninterrupted access without arranging assignment first. No live migration or assignment has been performed by this PR.

All existing and newly invited PharmacyStaff start with NO branch assignment. They are denied private branch operations until a pharmacy admin or platform admin uses Staff Management to select an active branch of the same pharmacy. Admins retain their existing pharmacy-wide scope. Staff cannot assign themselves. Clearing a selection revokes branch access. A staff member can have one branch; multiple-branch staff access is not supported. Assignment is read from the database on each request, so an existing JWT does not keep access to a former branch.

Controls cover branch inventory routes, branch operations/dashboard/queue/activity, branch status, reservation list/detail and pickup confirmation. Staff branch list is restricted and the pharmacy-private profile is denied to staff. Public pharmacy/branch directory and availability remain public. Customer-owned reservations remain customer-scoped. Public data is not converted into private data by these changes.

Update the API and UI together after the migration. No guessed mapping or automatic assignment of existing staff is provided. Assign using the admin UI, not ad-hoc SQL. Test two branches and an unassigned staff account before production adoption. Rolling back the migration loses the assignments; rolling back API code restores old broader staff access and is NOT a safe security fallback.

Known limitation: a request already in progress can complete as assignment changes. There is no multi-step transaction binding assignment changes to all inventory writes. This is not a replacement for TLS, per-user account security, or general per-token logout revocation. Existing old tests that mint JWTs for users never persisted to the database are no longer valid after current-account validation; the dedicated SQL HTTP tests persist real synthetic users.

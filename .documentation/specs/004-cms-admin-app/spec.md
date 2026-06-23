---
classification: full-spec
risk_level: high
risk_profile: internal
change_type: brownfield
archetype: web-service
target_workflow: specify-full
required_artifacts: spec, plan, tasks
recommended_next_step: plan
required_gates: checklist, analyze, critic
participants:
  owner: human
  planner: ai
  implementer: ai
  reviewer: human
  critic: ai
  scribe: ai
---

# Feature Specification: CMS Admin App

**Feature Branch**: `004-cms-admin-app`
**Created**: 2026-06-22
**Status**: Complete <!-- Valid: Draft | In Progress | Complete -->
**Input**: User description: "Create a lightweight CMS admin web app so site administrators and content editors can manage MakeBoldSpark CMS content (domains, blogs, authors, posts, categories, menus, keywords, content parts, subscribers, newsletters, and mail settings) through a visual interface instead of calling the API directly"

## Rationale Summary

### Core Problem

Today, managing MakeBoldSpark's CMS content (sites, blogs, posts, authors, categories, navigation, subscribers, newsletters, and mail configuration) requires calling the platform's API directly — there is no visual interface. This makes routine content work slow, error-prone (the platform's update operations are full-record replacements, so a partial edit can silently wipe other fields), and inaccessible to anyone not comfortable crafting raw HTTP requests.

### Decision Summary

Build a dedicated, lightweight administrative web application — bearing the organization's existing brand identity — focused on browsing and editing the CMS content the platform already manages, gated behind a real sign-in step that verifies an administrator's identity instead of merely checking that a credential is shaped correctly.

### Key Drivers

- Business driver: faster, lower-error content publishing and site-configuration workflow
- Technical constraint: the platform's content-management capabilities already exist and are complete; the one missing capability is a way for an administrator to actually sign in and be issued credentials the platform recognizes — the platform currently has no way to verify who is asking and will accept any well-formed access credential as proof of administrator identity
- User/operational impact: reduces mistakes caused by manual full-record update calls, removes the need for non-technical contributors to use API tooling, and closes a real gap where the platform cannot currently distinguish a legitimate administrator from anyone who knows the shape of an access credential

### Source Inputs

- Existing MakeBoldSpark CMS content-management capability (already supports full read/write access to all entity types named below)
- Platform's documented content model and known editing pitfalls (e.g., full-replace updates, fields that must never be displayed in plain text)
- The existing Author entity already models an email, a credential, and an administrator flag — the natural basis for a sign-in capability rather than introducing a separate account system
- The organization's official brand identity (logo, color palette, typography, and brand guide), which the platform's other public-facing pages already present consistently and which this feature should match rather than introduce its own look
- Prior specs `002-recipe-api` and `003-makeboldspark-api`, which establish this platform's API-first conventions this feature builds on top of

### Tradeoffs Considered

- Option A — keep using direct API calls / generic API-testing tools indefinitely: rejected, too error-prone and inaccessible for routine content work.
- Option B — build a fully bespoke editing experience tailored individually to each of the eleven content types: rejected for this iteration; the content types share a common shape (list, view, create, edit, delete) that doesn't justify eleven distinct designs yet.
- Option C — rely on the platform's current behavior of accepting any well-formed access credential without verifying who issued it: rejected as the sole gate for this feature, since an administrative tool meant to be reachable beyond a single trusted machine needs the platform to actually verify identity, not just check that a credential is shaped correctly.
- Option D — stand up a full external identity provider (e.g., a third-party sign-in service) before this feature can ship: rejected as disproportionate setup effort for a single-administrator tool when the platform already models the credential needed to verify identity directly.
- Selected — one consistent management interface covering all eleven content types, plus a minimal sign-in capability that verifies an administrator's existing credential and issues a properly verifiable access credential in return.

### Architectural Impact

- Adds a new user-facing surface to the platform.
- Adds one new platform capability: verifying an administrator's sign-in credential and issuing a verifiable access credential. This is the first time the platform will actually confirm who is making a request rather than only checking that a credential is shaped correctly — existing read/write content operations are otherwise unchanged.
- No backward-compatibility concerns — nothing already in production changes for existing API consumers.
- Introduces a new dependency: the platform must now be able to verify, not just parse, access credentials. Any other tool that already relies on today's "any well-formed credential is accepted" behavior should be reviewed once that gap closes.

### Reviewer Guidance

This spec now includes a real sign-in capability (not just content management), which touches authentication directly — flag this for explicit security review per project policy before implementation, particularly around credential storage and access-credential issuance. Also confirm the scope (all eleven content types, single administrator access tier, no new roles) matches what's actually needed today, and weigh in on the three resolved clarifications below if anything looks off.

## Clarifications

### Session 2026-06-22

- Q: How rich does the editing experience for post/content-part body text need to be? → A: A format-while-you-type editing experience (formatting applied interactively, not by typing raw markup).
- Q: How should content that isn't separated by site/blog by default (posts, menus) be presented? → A: Require selecting a site/blog first before viewing or managing its posts or menus.
- Q: How strictly should the app guard against deleting a record other records still depend on? → A: Block the delete entirely and explain why until dependent records are removed or reassigned.
- Q: Should the sign-in endpoint apply any baseline throttling to repeated failed attempts, even though full account-lockout UX is out of scope? → A: Yes — apply a lightweight, invisible throttle (e.g., a short delay or attempt cap) to deter automated guessing, with no user-facing "locked out" state.
- Q: Are author email addresses guaranteed unique platform-wide, so a submitted email always matches at most one account for sign-in purposes? → A: Yes — author emails are already unique platform-wide; sign-in always resolves to at most one account.

## User Scenarios & Testing *(mandatory)*

### User Story 0 - Sign in as an administrator (Priority: P1) ✅ Complete

An administrator opens the admin app and signs in with the email and password already on their author profile. Only authors flagged as administrators can sign in. Once signed in, they can use the rest of the application until they sign out or their session expires.

**Why this priority**: Nothing else in this feature is reachable without this — it is the prerequisite for every other story, and it is the one genuine security gap this feature must close (today, the platform cannot verify who is asking).

**Independent Test**: Can be fully tested by attempting to sign in with a non-administrator author's credentials (rejected), an administrator's correct credentials (succeeds), and an administrator's incorrect password (rejected with a generic, non-specific error) — independent of any content-management functionality.

**Acceptance Scenarios**:

1. **Given** an author flagged as an administrator, **When** they enter their correct email and password, **Then** they are signed in and reach the content area.
2. **Given** an author who is not flagged as an administrator, **When** they enter their correct email and password, **Then** they are denied access with a message that does not confirm or deny whether the account exists.
3. **Given** any author, **When** they enter an incorrect password, **Then** they are denied access with the same generic message used for a non-administrator account, so failed attempts cannot be used to discover which accounts exist or which are administrators.
4. **Given** a signed-in administrator, **When** they choose to sign out, **Then** they can no longer perform any action in the application until they sign in again.

---

### User Story 1 - Manage core site content (Priority: P1) ✅ Complete

A content administrator logs into the admin app and manages the everyday publishing content: sites/domains, blogs, authors, and posts, plus the categories used to organize posts. They can see what exists, open any record to review or change it, add new records, and remove ones that are no longer needed.

**Why this priority**: This is the day-to-day work the tool exists for — without it, administrators still have to use API tooling for the most frequent task (publishing and maintaining articles).

**Independent Test**: Can be fully tested by logging in, creating a new blog post end-to-end (assigning it to an existing blog and author), editing it, and confirming it appears correctly — without touching any API tool. Delivers complete value on its own even if no other story ships.

**Acceptance Scenarios**:

1. **Given** an authenticated administrator on the content list for any of sites, blogs, authors, or categories — or on the post list after selecting a blog — **When** they select "create new," fill in the required fields, and save, **Then** the new record appears in that list with the values they entered.
2. **Given** an existing post, **When** the administrator opens it, changes its title and content, and saves, **Then** the change is reflected when the record is reopened, and no other field on the record was altered.
3. **Given** an existing record with no dependent records, **When** the administrator deletes it, **Then** it no longer appears in the list and a confirmation is shown before the deletion is finalized.

---

### User Story 2 - Manage site navigation and reusable content (Priority: P2) ✅ Complete

An administrator manages the navigation menus for a site, the keywords used for discovery/SEO, and reusable content blocks (content parts) that appear in multiple places on the published site.

**Why this priority**: Less frequent than day-to-day post editing, but still routine site-maintenance work that currently requires API access.

**Independent Test**: Can be fully tested by creating a new menu item under an existing site, nesting it under a parent menu item, and confirming the hierarchy displays correctly — independent of post/author management.

**Acceptance Scenarios**:

1. **Given** a site's existing menu items, **When** the administrator views the menu list, **Then** parent/child relationships between menu items are visibly represented, not just a flat list.
2. **Given** a new keyword or content part, **When** the administrator creates it, **Then** it becomes available for selection wherever the platform already links keywords/content parts to other records.

---

### User Story 3 - Manage subscribers, newsletters, and mail configuration (Priority: P3) ✅ Complete

An administrator reviews who is subscribed to a blog, records which posts have been sent as newsletters, and maintains the outgoing mail configuration used to send them.

**Why this priority**: Lower frequency and higher sensitivity (mail credentials, subscriber personal data) than content editing — valuable, but the tool delivers most of its value without it.

**Independent Test**: Can be fully tested by adding a subscriber, recording a newsletter send against an existing post, and updating mail configuration — independent of the content-management stories.

**Acceptance Scenarios**:

1. **Given** the subscriber list for a blog, **When** the administrator adds, edits, or removes a subscriber, **Then** the list reflects the change immediately.
2. **Given** a post that was sent as a newsletter, **When** the administrator records that send, **Then** it appears in the newsletter history and cannot subsequently be edited — only removed if recorded in error, consistent with newsletter records being a historical log rather than an editable document.
3. **Given** mail configuration containing a sending credential, **When** the administrator views it, **Then** the credential is masked by default and only revealed or changed through an explicit action.

---

### Edge Cases

- What happens when an administrator tries to delete a record that other records still depend on (e.g., a blog that still has posts, or a site that still has menu items)? The deletion is blocked entirely and the administrator is told what still depends on the record, so they can remove or reassign those records first.
- What happens when an administrator's session expires while they are mid-edit? The tool should clearly indicate that re-authentication is needed and avoid silently discarding unsaved work where feasible.
- What happens when an administrator assigns a record to a parent that doesn't exist (e.g., a post to a blog that was deleted in another session)? The tool should surface a clear, specific error rather than a generic failure.
- What happens when two administrators edit the same record at the same time? Because the platform's update model is a full-record replace, the most recently saved edit wins and silently supersedes the other — the tool should make this behavior discoverable (e.g., showing when a record was last changed) rather than hiding it.
- How should content spanning multiple sites or blogs be presented, given several content types (posts, menus) are not separated by site/blog unless explicitly filtered? The administrator selects a site (and, for posts, a blog within it) first; posts and menus are then shown scoped to that selection rather than mixed together across every site/blog.
- What happens when someone repeatedly tries incorrect sign-in credentials? The application must not reveal through its responses or timing whether a given email belongs to an existing or administrator account; it always returns the same generic failure message regardless of the reason (unknown email, wrong password, or not an administrator). It also applies an invisible baseline throttle to repeated attempts against the same account to deter automated guessing, without ever surfacing a "locked out" state to the caller.
- What happens if an administrator's password needs to change or they're locked out? Out of scope for this feature — see Out of Scope.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST require an administrator to sign in with their email and password before any session — including viewing existing content — begins.
- **FR-001a**: The application MUST reject sign-in for any author not flagged as an administrator, using the same generic failure message used for an incorrect password or unknown email, so the response never reveals which condition caused the rejection.
- **FR-001b**: The application MUST verify the submitted password against a securely stored, non-reversible form of the administrator's credential — the platform MUST NOT store or compare passwords in plain, reversible form.
- **FR-001c**: The application MUST end a session on sign-out or expiration such that no further action can be performed until the administrator signs in again.
- **FR-002**: The application MUST allow an authenticated administrator to view, create, edit, and delete records for each of: sites/domains, blogs, authors, posts, and categories.
- **FR-003**: The application MUST allow an authenticated administrator to view, create, edit, and delete records for: menus, keywords, and content parts, and MUST present menu items in a way that shows their parent/child relationships rather than as a flat, unordered list.
- **FR-004**: The application MUST allow an authenticated administrator to view, create, edit, and delete subscriber records, and to view, create, and delete (but not edit) newsletter records, consistent with newsletters being an append-only history rather than an editable document.
- **FR-005**: The application MUST allow an authenticated administrator to view, create, edit, and delete mail configuration records, and MUST mask any sending credential by default, requiring an explicit action to reveal or change it.
- **FR-006**: The application MUST never display an author's stored password value in plain text under any circumstance.
- **FR-007**: When an attempted change is rejected (e.g., an invalid reference to another record, or a delete blocked by dependent records), the application MUST present a specific, actionable message rather than a generic failure notice.
- **FR-008**: The application MUST ask for confirmation before completing any delete action.
- **FR-009**: The application MUST present the current values of a record before allowing an edit, so an administrator is never required to re-enter unrelated fields just to change one value.
- **FR-010**: The application MUST provide a format-while-you-type editing experience for the body text of posts and content parts, so administrators apply formatting (e.g., headings, bold, links) interactively rather than typing or pasting raw markup.
- **FR-011**: The application MUST require an administrator to select a site (and, for posts, a blog within that site) before viewing or managing that site's or blog's posts or menus, rather than presenting them mixed together across every site/blog by default.
- **FR-012**: The application MUST block a delete action and explain what still depends on the record, rather than completing the deletion, whenever other records depend on the record being deleted.
- **FR-013**: The application MUST present the organization's official brand identity (logo, color palette, and typography), consistent with how the platform's other public-facing pages already present it, rather than generic or unbranded styling.
- **FR-014**: The application MUST apply a baseline throttle to repeated failed sign-in attempts against the same account (e.g., a short delay or limited attempt count) to deter automated guessing, without presenting any user-facing "account locked" state — this is a behind-the-scenes safeguard, distinct from the account-lockout experience explicitly out of scope.

### Key Entities *(include if feature involves data)*

- **Site/Domain**: A top-level site configuration the platform serves content for; owns its own navigation menus.
- **Blog**: A publication belonging to a site; the container for posts, authors, subscribers, newsletters, and mail configuration.
- **Author**: A content creator who can be linked to one or more blogs; identified uniquely by email, with a display profile and a credential that must never be shown in plain text. Authors flagged as administrators use this same email and credential to sign into the admin application.
- **Post**: An individual article belonging to a blog and an author; can be grouped under one or more categories.
- **Category**: A topic grouping used to organize posts.
- **Menu**: A navigation item belonging to a site; menu items can be nested under other menu items to form a hierarchy.
- **Keyword**: A discovery/SEO tag that can be associated with menus and content parts.
- **Content Part**: A reusable block of content that can appear in multiple places on a published site.
- **Subscriber**: A person who has subscribed to receive updates from a blog.
- **Newsletter**: A historical record that a specific post was sent as a newsletter; once recorded, not editable, only removable.
- **Mail Configuration**: The outgoing-mail settings used to send a blog's newsletters, including a sending credential that must be masked by default.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An administrator can create a new post — assigning it to an existing blog and author — and confirm it was saved correctly, in under 3 minutes, without using any API-calling tool.
- **SC-002**: All eleven content types in scope (sites, blogs, authors, posts, categories, menus, keywords, content parts, subscribers, newsletters, mail configuration) are fully viewable, and each supports the create/edit/delete actions defined for it in the Functional Requirements.
- **SC-003**: An administrator can locate and open the editor for any specific record in under 30 seconds from signing in.
- **SC-004**: Attempted deletions blocked by dependent records produce a specific, actionable explanation in 100% of cases, rather than an unexplained failure.
- **SC-005**: A first-time administrator who already understands the platform's content model can make and confirm a successful content change within 10 minutes of first sign-in, without external documentation.
- **SC-006**: An administrator can sign in with their existing credential and reach the content area in under 15 seconds; a non-administrator account or incorrect password is rejected every time, with no observable difference in the response that would reveal why.

## Assumptions

- The application is for internal administrative use only — it is not part of the public-facing published site, and its users are the same people who already hold administrative access to the platform.
- A single administrator access tier is sufficient; the platform does not currently distinguish finer-grained content-editing permissions, and this feature does not introduce any.
- All eleven content types share a common list/view/create/edit/delete interaction pattern, with the specific exceptions already called out (menu hierarchy display, newsletters being append-only, and masked credential fields for authors and mail configuration).
- The organization's brand assets (logo files, fonts, color/style guide) already exist in the organization's brand asset library and have already been applied to the platform's other public-facing pages; this feature reuses that existing identity rather than designing new branding.
- Author email addresses are already unique platform-wide; sign-in can rely on email as the matching key without a tie-breaking rule for duplicates.
- No prior context was skipped or unavailable while drafting this spec.

## Out of Scope

- Rich-media asset management (uploading or browsing image/file libraries) beyond entering an existing reference to a file or URL.
- Bulk import/export of content.
- Multi-language or localization support.
- Change history, version control, or rollback of edits beyond the platform's existing record timestamps.
- The public-facing reader experience of the published site itself — this feature is the administrative tool, not the site visitors see.
- New user roles or permission tiers beyond the platform's existing administrator access.
- Self-service password reset, "forgot password" flows, and any user-facing "account locked" experience — an administrator whose password needs to change, or who is throttled, is handled outside this application for now (a behind-the-scenes throttle on repeated failed attempts is in scope; see FR-014).
- Multi-factor authentication and integration with any third-party identity provider.

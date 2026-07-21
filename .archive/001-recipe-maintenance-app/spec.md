---
classification: full-spec
risk_level: medium
archetype: web-service
risk_profile: internal
change_type: brownfield
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

# Feature Specification: Recipe Maintenance Application

**Feature Branch**: `001-recipe-maintenance-app`
**Created**: 2026-06-23
**Status**: Complete
**Input**: Create a separate recipe maintenance application that follows the established CMS experience and uses the established recipe-management service.

## Rationale Summary

### Core Problem

Recipe publishers need a focused workspace for maintaining recipe content and its category taxonomy. The existing general CMS does not provide this dedicated workflow.

### Decision Summary

Provide a distinct recipe maintenance application, named `MakeBoldSpark.Recipe.Client`, that follows the established CMS interaction model while using the existing recipe-management service as the sole source of recipe and category data. The application will preserve the platform's existing authorization boundary for publishing changes.

### Key Drivers

- Give recipe publishers a focused workflow instead of requiring them to manage recipe data through unrelated content screens.
- Keep recipe and category records authoritative in the existing recipe-management service.
- Reuse familiar sign-in, navigation, editing, confirmation, and session-recovery behaviours from the CMS.

### Source Inputs

- User request for a separate recipe maintenance application.
- Existing CMS interaction patterns.
- Existing Recipe API specification and authorization policy.
- MakeBoldSpark Constitution v1.2.0.

### Tradeoffs Considered

- Extending the general CMS would centralize maintenance but would continue to mix recipe work with unrelated content administration.
- Creating an independent management surface duplicates some navigation and session behaviours but gives recipe publishers a focused workflow.
- Selected: a separate recipe maintenance application that shares the established CMS experience.

### Architectural Impact

- Adds a separate management surface without creating a second backend platform or a duplicate recipe data store.
- The established recipe-management service remains the authoritative source for recipe and category records.
- Existing public recipe browsing remains unchanged.

### Reviewer Guidance

Verify that only authorized publishers can change content, recipe and category workflows are complete, failed or expired sessions protect unsaved work, and the app does not introduce a competing source of recipe data.

### Assumptions

- The established sign-in flow can issue credentials accepted for recipe publishing by authorized administrators and publishers.
- “Same approach as MakeBoldSpark.Cms” means consistent user workflows and session behaviour, not that the two applications must share a codebase or deployment.
- `MakeBoldSpark.Recipe.Client` is the distinct project identifier for the recipe-maintenance application; `MakeBoldSpark.Recipe` remains the recipe domain library.
- Context was gathered from the constitution and the two existing related specifications; no context sources were skipped.

### Out of Scope

- Public recipe browsing and public-facing site presentation.
- Recipe search, reporting, ratings, comments, and analytics.
- Recipe image upload or storage workflows.
- Changes to the general CMS's existing content-management scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Maintain Recipe Content (Priority: P1) ✅ Complete

As an authorized recipe publisher, I want to view, create, edit, and remove recipes so that recipe content remains accurate and current.

**Why this priority**: Recipe maintenance is the primary value of the application.

**Independent Test**: An authorized publisher can sign in, select a recipe, update its content and publication details, save it, and confirm the saved result appears in the recipe list.

**Acceptance Scenarios**:

1. **Given** an authorized publisher is signed in, **when** they open the recipe workspace, **then** they can see the recipes available for maintenance and start a new recipe.
2. **Given** an authorized publisher is editing a recipe, **when** they save valid changes, **then** the application confirms the save and shows the updated record.
3. **Given** an authorized publisher chooses to remove a recipe, **when** they confirm the destructive action, **then** the recipe is removed and no longer appears in the maintenance list.

---

### User Story 2 - Maintain Recipe Categories (Priority: P2) ✅ Complete

As an authorized recipe publisher, I want to manage recipe categories so that recipes can be organized consistently.

**Why this priority**: Category maintenance supports accurate recipe classification but depends on the primary recipe workflow.

**Independent Test**: An authorized publisher can create a category, update its name and display details, and remove it after confirmation.

**Acceptance Scenarios**:

1. **Given** an authorized publisher is signed in, **when** they open category maintenance, **then** they can see and edit the available categories.
2. **Given** an authorized publisher saves a valid new category, **when** the save completes, **then** the category is available for recipe classification.

---

### User Story 3 - Recover from an Expired Session (Priority: P3) ✅ Complete

As an authorized recipe publisher, I want to reauthenticate without losing an open edit so that an expired session does not force me to re-enter work.

**Why this priority**: This protects content-entry work and matches the established CMS experience.

**Independent Test**: During an unsaved edit, simulate an expired session, sign in again, and verify the open edit remains available for saving.

**Acceptance Scenarios**:

1. **Given** a publisher has unsaved recipe or category changes, **when** their session expires, **then** the application asks them to sign in again without discarding the open changes.

### Edge Cases

- A recipe or category is removed by another publisher after it has been opened for editing.
- A save is rejected because required recipe or category information is missing or invalid.
- The recipe-management service is unavailable while a publisher loads or saves data.
- An unauthorized or expired session attempts a maintenance action.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a separate recipe-maintenance application with a dedicated landing view and navigation for recipes and recipe categories.
- **FR-002**: The system MUST require an authenticated, authorized publisher before displaying maintenance functions or accepting any recipe or category changes.
- **FR-003**: The system MUST use the established recipe-management service as the sole source of recipe and category records, without creating or maintaining a duplicate data source.
- **FR-004**: Authorized publishers MUST be able to view the recipes available for maintenance, create recipes, edit recipe content and classification, set publication status, and permanently remove recipes after an explicit confirmation.
- **FR-005**: The recipe list MUST include every recipe available to the publisher, including recipes that are not approved for public display.
- **FR-006**: Authorized publishers MUST be able to view, create, edit, and permanently remove recipe categories after an explicit confirmation; a category with assigned recipes MUST NOT be removable until the publisher has reassigned or removed those recipes.
- **FR-007**: The system MUST show clear, actionable outcomes for successful saves, validation failures, missing records, unavailable service responses, and denied actions.
- **FR-008**: When a session expires during an open recipe or category edit, the system MUST support reauthentication without discarding unsaved changes.
- **FR-009**: The system MUST prevent unconfirmed destructive actions from deleting recipe or category records.
- **FR-010**: The system MUST reject a save when a recipe or category has changed since the publisher opened it, preserve the publisher's unsaved values, and require the publisher to reload the latest record before saving again.
- **FR-011**: The system MUST require a publisher to select an active domain before maintenance and MUST limit recipe and category lists, views, and changes to that selected domain.
- **FR-012**: Any authorized publisher MUST be able to select any configured domain as the active domain; domain access remains governed by the established publisher authorization model.

### Key Entities

- **Recipe**: A maintained food or drink entry with descriptive content, preparation information, classification, publication status, and associated display metadata.
- **Recipe Category**: A domain-specific named organizational grouping used to classify recipes, with display details and active status.
- **Active Domain**: The publisher-selected site context that bounds recipe and category maintenance.
- **Publisher Session**: The authenticated authorization context that permits a publisher to view maintenance data and submit changes.

## Clarifications

### Session 2026-06-23

- Q: Should the maintenance list include recipes that are not approved for public display? → A: Yes. Authorized publishers maintain all recipes, including unapproved records.
- Q: How should category deletion handle assigned recipes? → A: Block deletion until the publisher reassigns or removes all associated recipes.
- Q: How should the system handle a record changed by another publisher during an edit? → A: Reject the stale save, preserve unsaved values, and require a reload of the latest record.
- Q: Should maintenance be limited to an active domain or combine all domains? → A: Require a publisher-selected active domain and limit maintenance to that domain.
- Q: Which domains may an authorized publisher select? → A: Any configured domain, using the established role-based publisher authorization model.
- Q: What should the recipe-maintenance application's project identifier be? → A: `MakeBoldSpark.Recipe.Client`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In usability testing, at least 90% of authorized publishers can create and publish a recipe, including category selection, in under 3 minutes on their first attempt.
- **SC-002**: In acceptance testing, 100% of valid recipe and category saves show a confirmed updated result to the publisher within 5 seconds.
- **SC-003**: In authorization testing, 100% of unauthenticated or unauthorized recipe and category change attempts are denied without changing records.
- **SC-004**: In session-recovery testing, 100% of simulated expired-session edits retain the publisher's unsaved recipe or category values after successful reauthentication.

# Data Model: Recipe Maintenance Application

## Recipe

Existing persisted recipe record, scoped by `DomainId` and assigned to exactly one recipe category.

| Field | Rules for maintenance |
| --- | --- |
| Id | Server-generated identity; immutable after creation. |
| DomainId | Required active-domain identifier; immutable after creation through this client. |
| Name | Required; maximum 150 characters. |
| Description | Optional; maximum 500 characters. |
| AuthorName | Required; maximum 50 characters. |
| Ingredients / Instructions | Required text. |
| Servings | Non-negative integer. |
| RecipeCategoryId | Required; must reference a category in the same active domain. |
| IsApproved | Publisher-controlled publication state. |
| Version | Server-managed positive integer used for optimistic concurrency. |
| UpdatedDate | Server-managed display/audit timestamp; not the concurrency precondition. |

**Lifecycle**: Draft/unapproved → approved → unapproved; delete is permanent only after confirmation and a matching version.

## Recipe Category

Existing persisted category record, scoped by `DomainId`.

| Field | Rules for maintenance |
| --- | --- |
| Id | Server-generated identity; immutable after creation. |
| DomainId | Required active-domain identifier; immutable after creation through this client. |
| Name | Required; maximum 70 characters; unique within a domain. |
| Description | Optional; maximum 1,500 characters. |
| DisplayOrder | Integer display ordering value. |
| IsActive | Publisher-controlled availability state. |
| Version | Server-managed positive integer used for optimistic concurrency. |

**Lifecycle**: Active ↔ inactive; delete only when no recipes reference the category and the supplied version matches. The reference check and delete are one atomic persistence operation; a database relationship-restrict violation is mapped to the same in-use conflict result.

## Active Domain

The selected configured-domain record that bounds every list, detail, create, update, and delete action. It is selected by the authenticated publisher, is not duplicated in the recipe database, and is passed to the Recipe API with every maintenance request.

## Publisher Session

The existing signed access token, display name, and expiration timestamp held in browser session storage. A 401 activates the reauthentication prompt while preserving the current editor state. The token is never written to a URL, log payload, source file, or persisted recipe record.

## Relationships and Integrity

```text
Active Domain 1 ── * Recipe Category 1 ── * Recipe
Active Domain 1 ── * Recipe
Publisher Session ── selects ── 1 Active Domain
```

- A recipe category cannot be deleted while recipes reference it, including when a recipe is assigned concurrently with the delete attempt.
- A recipe cannot be assigned to a category from another domain.
- A detail, update, or delete request for a record outside the active domain returns not found rather than revealing cross-domain data.
- Version checks apply to update and delete operations; a mismatch returns a conflict without persisting any partial change.

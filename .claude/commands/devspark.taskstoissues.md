---
description: Convert existing tasks into actionable, dependency-ordered GitHub issues for the feature based on available design artifacts.
tools: ['github/github-mcp-server/issue_write']
scripts:
  sh: .devspark/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
  ps: .devspark/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
---

## Prompt Resolution

Determine the current git user by running `git config user.name`.
Normalize to a folder-safe slug: lowercase, replace spaces with hyphens, strip non-alphanumeric/hyphen chars.

Read and execute the instructions from the **first file that exists**:
1. `.documentation/{git-user}/commands/devspark.taskstoissues.md` (personalized override)
2. `.documentation/commands/devspark.taskstoissues.md` (team customization)
3. `.devspark/defaults/commands/devspark.taskstoissues.md` (stock default)

Where `{git-user}` is the normalized slug from step above.

## User Input

```text
$ARGUMENTS
```

Pass the user input above to the resolved prompt.

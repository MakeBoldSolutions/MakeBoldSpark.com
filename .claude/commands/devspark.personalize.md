---
description: Create a personalized copy of any DevSpark command prompt for the current git user.
scripts:
  sh: .devspark/scripts/bash/check-prerequisites.sh --json
  ps: .devspark/scripts/powershell/check-prerequisites.ps1 -Json
---

## Prompt Resolution

Determine the current git user by running `git config user.name`.
Normalize to a folder-safe slug: lowercase, replace spaces with hyphens, strip non-alphanumeric/hyphen chars.

Read and execute the instructions from the **first file that exists**:
1. `.documentation/{git-user}/commands/devspark.personalize.md` (personalized override)
2. `.documentation/commands/devspark.personalize.md` (team customization)
3. `.devspark/defaults/commands/devspark.personalize.md` (stock default)

Where `{git-user}` is the normalized slug from step above.

## User Input

```text
$ARGUMENTS
```

Pass the user input above to the resolved prompt.

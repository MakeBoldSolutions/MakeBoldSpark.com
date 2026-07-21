# Quickstart: Recipe Maintenance Application

## Prerequisites

- .NET 10 SDK and Node.js version required by the existing CMS client lockfile.
- Local Recipe and MakeBoldSpark database configuration supplied through user secrets or environment settings; never commit production credentials.
- A test user with the `Publisher` or `Admin` role.

## Development flow

1. Restore packages and build the solution from the repository root.
2. Run the Recipe database migration against a disposable copy of the local database, verify the pre-migration backup, and seed at least two domains, recipes, and categories.
3. Start `MakeBoldSpark.Api`; open `/recipes` and sign in with the publisher test user.
4. Select a domain and verify the recipe list includes unapproved recipes in that domain only.
5. Create, edit, approve/unapprove, and delete a recipe. Verify browser confirmation is required before deletion.
6. Attempt to delete a category with recipes; verify the request returns a clear in-use message and leaves all data unchanged.
7. Open the same recipe in two sessions; save in the first session, then save in the second. Verify the second receives a conflict and retains its unsaved values.
8. Expire the browser session while editing; sign in again and verify the editor state remains available.

## Validation commands

```powershell
dotnet test .\tests\MakeBoldSpark.Api.Tests\MakeBoldSpark.Api.Tests.csproj
.\scripts\powershell\verify-recipe-migration.ps1 -DryRun
dotnet list .\MakeBoldSpark.slnx package --vulnerable
Push-Location .\src\MakeBoldSpark.Recipe.Client\client
npm ci
npm audit --omit=dev
npm run lint
npm test
npm run build
Pop-Location
dotnet build .\MakeBoldSpark.slnx
```

## Release checks

- Compare the generated OpenAPI document with the feature contract and verify every publisher status response in the contract test suite.
- Confirm `scripts/powershell/verify-recipe-migration.ps1` passes against a disposable copy of the Recipe database, including backup verification, rehearsal migration, post-migration integrity checks, and the documented forward-fix procedure.
- Verify liveness, readiness, request/error/duration telemetry, and correlation IDs for publisher operations; alert when publisher recipe p95 latency approaches 5 seconds for 5 minutes, 5xx responses exceed 1% for 5 minutes, 4xx responses double the 24-hour baseline, or mutation rate-limit rejections remain above 10 per minute.
- Confirm no database file, connection string, token, or production secret is included in source control or deployment output.
- Smoke-test `/recipes`, protected publisher endpoints, `/api/health`, and the existing `/cms` application after deployment.

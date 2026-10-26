# Dependency security update — 2026-10-26

The application previously resolved two vulnerable transitive packages:

| Package | Previous | Pinned patched version | Advisory |
| --- | --- | --- | --- |
| Microsoft.OpenApi | 2.0.0 | 2.7.5 | [GHSA-v5pm-xwqc-g5wc](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc) |
| System.Security.Cryptography.Pkcs | 9.0.4 | 9.0.21 | [GHSA-846f-f764-76mx](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx), [GHSA-2fmv-rv2c-59x7](https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7) |

Explicit package references raise the dependency minimums while preserving the existing major versions. OpenAPI is brought in by ASP.NET Core OpenAPI; PKCS is brought in through Entity Framework SQL Server and Microsoft.Data.SqlClient. Keep the pins until the parent dependencies require patched versions themselves.

Validation:

- Release build: zero warnings and zero errors.
- NuGet audit including transitive packages: no known vulnerable packages reported.
- OpenAPI HTTP response: 200, with 39 documented paths.
- Existing database/browser integration suite passed against the separate test LocalDB: topic and question edits, add/delete/reset/import, publication, student submission, teacher review, CSV export, persistence in a fresh browser session, and rejected invalid/stale/unauthorized/locked writes.

The update changes package references only; it requires no database migration. These results describe the advisory data available on the verification date, not a guarantee against future advisories.

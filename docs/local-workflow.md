# Local operation on Windows

Requires .NET 10 SDK, Python 3, SQL Server LocalDB and `sqlcmd`. The commands work with Windows PowerShell 5.1 and PowerShell 7. Run each command separately from the repository root.

```powershell
Set-Location 'C:\Users\konsu\Dev\ReDI-assessment'
```

Start both parts of the application:

```powershell
.\redi.ps1 start
```

The command builds the backend, starts it and the frontend in the background, and waits for their ports. It does not apply migrations. Open http://127.0.0.1:8000/admin.html for the teacher dashboard or http://127.0.0.1:8000/evaluation.html for the configured current test. Share the assessment-specific link from the dashboard for another published test. The ASP.NET development certificate must be trusted by your browser as with the previous manual launch.

```powershell
.\redi.ps1 status
```

```powershell
.\redi.ps1 stop
```

Repeating `start` with the same settings does not create duplicate processes. `stop` checks the process ID, creation time and checkout path before stopping anything. Processes launched manually or from another checkout are not stopped. If an old manually launched application occupies a required port, stop it in its own terminal first. Background logs and process records are in the ignored `.local/` directory.

## Back up the database

```powershell
.\redi.ps1 backup
```

This creates a uniquely named COPY_ONLY `.bak` file under `.local/backups/`, with checksums, and asks SQL Server to verify it. A successful result prints the full file path. It works while the app is running. Keep a copy on another drive if you need protection against loss of this computer. Files under `.local/` are not uploaded to GitHub.

Choose another destination if needed:

```powershell
.\redi.ps1 backup -BackupDirectory 'D:\ReDI-backups'
```

## Restore without overwriting the existing database

Use the actual path printed by `backup`:

```powershell
.\redi.ps1 restore -BackupFile 'C:\path\to\your-backup.bak'
```

Restore verifies the backup, creates a new database named `ReDI_Restore_<timestamp>`, and runs SQL Server's integrity check. It refuses an existing database name or existing destination data files; it never uses `WITH REPLACE`. The original database remains available. Choose a specific unused name with `-Database`.

To inspect the restored database, first stop the application, then use the restored database name printed by the command:

```powershell
.\redi.ps1 stop
```

```powershell
.\redi.ps1 start -Database ReDI_Restore_20261026_140000
```

The connection override is passed only to that running application; configuration files are not edited. Return to the normal database with `stop` followed by `start` without `-Database`. Backing up a restored database requires its name explicitly with `backup -Database <name>`; plain `backup` always selects the configured normal database.

## Test environment

The advanced port and HTTP-only options are for local integration tests. They let a separate checkout run alongside the normal application:

```powershell
.\redi.ps1 start -Database ReDI_Consolidation_Test_20261026_7c91 -HttpPort 15069 -HttpsPort 17101 -FrontendPort 18000 -HttpOnly
```

Use the existing separate test database after applying its migrations as described in [tests/README.md](../tests/README.md). Do not point these tests at the normal application database. Stop the test checkout with its own `redi.ps1 stop` when finished.

## Verification record (2026-10-26)

Tested actual start/stop in Windows PowerShell 5.1 and PowerShell 7 using a checkout path containing spaces. Repeated start reused the same processes. Stop left the independently running normal application reachable. A start against its occupied ports was rejected without stopping it.

A test database backup was created with checksums, verified, restored to `ReDI_Tools_Restore_20261026`, and passed DBCC CHECKDB. Starting the app against that restored copy returned questions successfully. A second restore to the existing destination was rejected. No application database was overwritten. The three-student browser workflow and both exports passed against the isolated test database.

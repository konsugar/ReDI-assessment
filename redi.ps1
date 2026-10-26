[CmdletBinding()]
param(
    [Parameter(Position=0)][ValidateSet('start','stop','status','backup','restore')][string]$Action = 'status',
    [string]$Database,
    [string]$BackupFile,
    [string]$BackupDirectory,
    [ValidateRange(1024,65535)][int]$HttpPort = 5069,
    [ValidateRange(1024,65535)][int]$HttpsPort = 7101,
    [ValidateRange(1024,65535)][int]$FrontendPort = 8000,
    [switch]$HttpOnly
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($PSScriptRoot)
$local = Join-Path $root '.local'
$statePath = Join-Path $local 'processes.json'
$dll = Join-Path $root 'backend\bin\Release\net10.0\ReDI.Assessment.Api.dll'
$frontend = Join-Path $root 'frontend'
New-Item -ItemType Directory -Path $local -Force | Out-Null

function Read-State {
    if (Test-Path -LiteralPath $statePath) { return Get-Content -Raw -LiteralPath $statePath | ConvertFrom-Json }
    return $null
}
function Owned-Process($record, [string]$kind) {
    if ($null -eq $record) { return $null }
    $process = Get-Process -Id $record.Pid -ErrorAction SilentlyContinue
    if ($null -eq $process) { return $null }
    if ($process.StartTime.ToUniversalTime().Ticks.ToString() -ne $record.StartTicks) { return $null }
    $command = (Get-CimInstance Win32_Process -Filter "ProcessId=$($process.Id)").CommandLine
    $marker = if ($kind -eq 'backend') { $dll } else { $frontend }
    if (-not $command -or $command.IndexOf($marker,[StringComparison]::OrdinalIgnoreCase) -lt 0) { return $null }
    return $process
}
function Port-Open([int]$port) {
    $client = New-Object Net.Sockets.TcpClient
    try { return $client.ConnectAsync('127.0.0.1',$port).Wait(300) -and $client.Connected }
    catch { return $false }
    finally { $client.Dispose() }
}
function Write-State($state) {
    $temporary = "$statePath.tmp"
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $temporary -Encoding UTF8
    Move-Item -LiteralPath $temporary -Destination $statePath -Force
}
function Stop-Owned($state) {
    if ($null -eq $state -or $state.Root -ne $root) { return }
    foreach ($kind in @('backend','frontend')) {
        $process = Owned-Process $state.$kind $kind
        if ($null -ne $process) { Stop-Process -Id $process.Id; $process.WaitForExit(5000) | Out-Null }
    }
}
function Local-Connection {
    $config = Get-Content -Raw -LiteralPath (Join-Path $root 'backend\appsettings.json') | ConvertFrom-Json
    $builder = New-Object System.Data.Common.DbConnectionStringBuilder
    $builder.set_ConnectionString([string]$config.ConnectionStrings.DefaultConnection)
    $instance = if ($builder.ContainsKey('Server')) { [string]$builder['Server'] } else { [string]$builder['Data Source'] }
    $name = if ($Database) { $Database } elseif ($builder.ContainsKey('Database')) { [string]$builder['Database'] } else { [string]$builder['Initial Catalog'] }
    if ($instance -notmatch '^\(localdb\)\\[A-Za-z0-9_]+$') { throw 'These local tools support Windows LocalDB only.' }
    if ($name -notmatch '^[A-Za-z][A-Za-z0-9_]{0,127}$') { throw 'Use a database name containing letters, numbers and underscores.' }
    return @{Instance=$instance;Name=$name;Connection="Server=$instance;Database=$name;Trusted_Connection=True;TrustServerCertificate=True"}
}
function Sql([string]$query) {
    $command = Get-Command sqlcmd -ErrorAction Stop
    $output = & $command.Source -S $connection.Instance -E -b -r 1 -W -h -1 -s '|' -Q "SET NOCOUNT ON; $query"
    if ($LASTEXITCODE -ne 0) { throw 'SQL Server operation failed. See the SQL Server message above.' }
    return $output
}
function Quote-Sql([string]$value) { return $value.Replace("'","''") }

$lock = $null
try {
    try { $lock = [IO.File]::Open((Join-Path $local 'operation.lock'), 'OpenOrCreate', 'ReadWrite', 'None') }
    catch { throw 'Another ReDI command is running. Wait for it to finish.' }
    $state = Read-State
    switch ($Action) {
        'status' {
            foreach ($kind in @('backend','frontend')) {
                $running = if ($state -and $state.Root -eq $root) { $null -ne (Owned-Process $state.$kind $kind) } else { $false }
                Write-Output "$kind`: $(if ($running) {'running'} else {'not managed / stopped'})"
            }
            if ($state) { Write-Output "Database: $($state.Database)" }
        }
        'stop' {
            Stop-Owned $state
            if (Test-Path -LiteralPath $statePath) { Remove-Item -LiteralPath $statePath }
            Write-Output 'ReDI processes started by this checkout are stopped.'
        }
        'start' {
            if (@($HttpPort,$HttpsPort,$FrontendPort | Select-Object -Unique).Count -ne 3) { throw 'Use different ports for each endpoint.' }
            $connection = Local-Connection
            $backendRunning = $state -and ($null -ne (Owned-Process $state.backend 'backend'))
            $frontendRunning = $state -and ($null -ne (Owned-Process $state.frontend 'frontend'))
            if ($backendRunning -and $frontendRunning) {
                if ($state.Database -ne $connection.Name -or $state.HttpPort -ne $HttpPort -or $state.HttpsPort -ne $HttpsPort -or $state.FrontendPort -ne $FrontendPort -or [bool]$state.HttpOnly -ne [bool]$HttpOnly) { throw 'ReDI is already running with different settings. Run .\redi.ps1 stop first.' }
                Write-Output "Already running: http://127.0.0.1:$FrontendPort/admin.html"
                break
            }
            Stop-Owned $state
            $ports = @($HttpPort,$FrontendPort)
            if (-not $HttpOnly) { $ports += $HttpsPort }
            foreach ($port in $ports) { if (Port-Open $port) { throw "Port $port is occupied. Stop that application first; no unrelated process was stopped." } }
            $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
            $python = (Get-Command python -ErrorAction Stop).Source
            & $dotnet build (Join-Path $root 'backend\ReDI.Assessment.Api.csproj') --configuration Release --nologo --verbosity quiet
            if ($LASTEXITCODE -ne 0) { throw 'Build failed; ReDI was not started.' }
            $urls = "http://localhost:$HttpPort"
            if (-not $HttpOnly) { $urls = "https://localhost:$HttpsPort;$urls" }
            $newState = @{Root=$root;Database=$connection.Name;HttpPort=$HttpPort;HttpsPort=$HttpsPort;FrontendPort=$FrontendPort;HttpOnly=[bool]$HttpOnly}
            try {
                $process = Start-Process -FilePath $dotnet -ArgumentList @(('"{0}"' -f $dll),'--environment','Development','--urls',('"{0}"' -f $urls),'--ConnectionStrings:DefaultConnection',('"{0}"' -f $connection.Connection)) -WorkingDirectory (Join-Path $root 'backend') -RedirectStandardOutput (Join-Path $local 'backend.log') -RedirectStandardError (Join-Path $local 'backend-error.log') -WindowStyle Hidden -PassThru
                $newState.backend = @{Pid=$process.Id;StartTicks=$process.StartTime.ToUniversalTime().Ticks.ToString()}
                Write-State $newState
                $process = Start-Process -FilePath $python -ArgumentList @('-m','http.server',"$FrontendPort",'--bind','127.0.0.1','--directory',('"{0}"' -f $frontend)) -WorkingDirectory $root -RedirectStandardOutput (Join-Path $local 'frontend.log') -RedirectStandardError (Join-Path $local 'frontend-error.log') -WindowStyle Hidden -PassThru
                $newState.frontend = @{Pid=$process.Id;StartTicks=$process.StartTime.ToUniversalTime().Ticks.ToString()}
                Write-State $newState
                $deadline = [DateTime]::UtcNow.AddSeconds(30)
                do {
                    if (-not (Owned-Process $newState.backend 'backend') -or -not (Owned-Process $newState.frontend 'frontend')) { throw "A ReDI process stopped. Check the logs in $local." }
                    $ready = @($ports | Where-Object { -not (Port-Open $_) }).Count -eq 0
                    if ($ready) { break }
                    Start-Sleep -Milliseconds 300
                } while ([DateTime]::UtcNow -lt $deadline)
                if (-not $ready) { throw "Startup timed out. Check the logs in $local." }
                Write-Output "Teacher: http://127.0.0.1:$FrontendPort/admin.html"
                Write-Output "Student: http://127.0.0.1:$FrontendPort/evaluation.html"
                Write-Output "Database: $($connection.Name)"
            } catch {
                Stop-Owned ([PSCustomObject]$newState)
                if (Test-Path -LiteralPath $statePath) { Remove-Item -LiteralPath $statePath }
                throw
            }
        }
        'backup' {
            $connection = Local-Connection
            if (-not $BackupDirectory) { $BackupDirectory = Join-Path $local 'backups' }
            New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
            $name = '{0}_{1}_{2}.bak' -f $connection.Name,(Get-Date -Format 'yyyyMMdd_HHmmss'),([guid]::NewGuid().ToString('N').Substring(0,8))
            $file = [IO.Path]::GetFullPath((Join-Path $BackupDirectory $name))
            $escaped = Quote-Sql $file
            Sql "BACKUP DATABASE [$($connection.Name)] TO DISK=N'$escaped' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;" | Write-Output
            Write-Output "Verified backup: $file"
        }
        'restore' {
            if (-not $BackupFile -or -not (Test-Path -LiteralPath $BackupFile -PathType Leaf)) { throw 'Specify an existing .bak file with -BackupFile.' }
            if (-not $Database) { $Database = 'ReDI_Restore_' + (Get-Date -Format 'yyyyMMdd_HHmmss') }
            $connection = Local-Connection
            $file = (Resolve-Path -LiteralPath $BackupFile).Path
            $escaped = Quote-Sql $file
            $exists = @(Sql "SELECT CASE WHEN DB_ID(N'$($connection.Name)') IS NULL THEN 0 ELSE 1 END;") -join ''
            if ($exists.Trim() -ne '0') { throw 'The destination database already exists. Choose a new name with -Database; existing databases are never overwritten.' }
            Sql "RESTORE VERIFYONLY FROM DISK=N'$escaped' WITH CHECKSUM;" | Write-Output
            $dataPath = (@(Sql "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'));") -join '').Trim()
            if (-not $dataPath -or $dataPath -eq 'NULL') { throw 'SQL Server did not report its data directory.' }
            $rows = @(Sql "RESTORE FILELISTONLY FROM DISK=N'$escaped';")
            $moves = @(); $index=0
            foreach ($row in $rows) {
                $fields = $row -split '\|'
                if ($fields.Count -lt 3 -or $fields[2].Trim() -notin @('D','L')) { continue }
                $index++
                $extension = if ($fields[2].Trim() -eq 'L') { 'ldf' } else { 'mdf' }
                $destination = Join-Path $dataPath "$($connection.Name)_$index.$extension"
                if (Test-Path -LiteralPath $destination) { throw "Destination file already exists: $destination" }
                $moves += "MOVE N'$(Quote-Sql $fields[0].Trim())' TO N'$(Quote-Sql $destination)'"
            }
            if ($moves.Count -lt 2) { throw 'The backup file list was incomplete; no restore was attempted.' }
            Sql "RESTORE DATABASE [$($connection.Name)] FROM DISK=N'$escaped' WITH $($moves -join ','),CHECKSUM,RECOVERY; DBCC CHECKDB ([$($connection.Name)]) WITH NO_INFOMSGS;" | Write-Output
            Write-Output "Restored and checked database: $($connection.Name)"
            Write-Output "To inspect it, stop ReDI and run: .\redi.ps1 start -Database $($connection.Name)"
        }
    }
} finally { if ($null -ne $lock) { $lock.Dispose() } }

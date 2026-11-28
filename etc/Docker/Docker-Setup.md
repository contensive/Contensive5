# Docker Setup for Contensive (Redis, and optionally SQL Server)

Run Redis, and optionally SQL Server, in local Docker containers on a Windows server or developer machine. No code changes are needed: Contensive only reads different values from `config.json`.

- **Redis** gives Contensive a cache that survives IIS recycles and is shared by the IIS site and the Contensive Task Service.
- **SQL Server in Docker** is optional. Use it for development, test and staging servers that don't have their own SQL Server.

Commands are PowerShell. Run them from an **elevated** (Run as administrator) prompt unless a step says otherwise.

## Contents
1. [Install WSL 2](#1-install-wsl-2)
2. [Install Docker Desktop](#2-install-docker-desktop)
3. [Start Docker automatically (Task Scheduler)](#3-start-docker-automatically-task-scheduler)
4. [Create the compose file](#4-create-the-compose-file)
5. [Start and verify the containers](#5-start-and-verify-the-containers)
6. [Point Contensive at Redis (config.json)](#6-point-contensive-at-redis-configjson)
7. [Optional: point Contensive at SQL Server in Docker (config.json)](#7-optional-point-contensive-at-sql-server-in-docker-configjson)
8. [Daily operations](#8-daily-operations)
9. [Working with sites](#9-working-with-sites)
10. [Site snapshots](#10-site-snapshots-database)
11. [Switching between Docker and cloud services](#11-switching-between-docker-and-cloud-services)
12. [GitHub Actions CI](#12-github-actions-ci)
13. [Troubleshooting](#13-troubleshooting)

## Prerequisites

- Windows 11, Windows 10 22H2, or Windows Server 2022 or later, with administrator rights.
- Hardware virtualization turned on.
  - On a physical machine: in the BIOS/UEFI.
  - On a virtual machine: the host must support **nested virtualization**. Many cloud VM types do not; check your provider. If WSL 2 can't start, Docker Desktop can't run.
- Contensive installed, with the CLI (`cc`) at `C:\Program Files\Contensive\Cli\`.
- Docker Desktop licensing: it is free for personal use, education and small businesses. Larger organizations need a paid Docker subscription. Check Docker's current terms.

---

## 1. Install WSL 2

Docker Desktop runs Linux containers inside WSL 2 (Windows Subsystem for Linux).

1. Install WSL without a Linux distribution. Docker Desktop brings its own.

    ```powershell
    wsl --install --no-distribution
    ```

2. **Restart Windows.**

3. After the restart, set version 2 as the default and update the WSL kernel:

    ```powershell
    wsl --set-default-version 2
    wsl --update
    wsl --status
    ```

    `wsl --status` must show `Default Version: 2`.

If `wsl --install` is not recognized, the Windows build is too old. Turn on the features by hand, restart, then run step 3:

```powershell
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
```

## 2. Install Docker Desktop

1. Install Docker Desktop, either with winget:

    ```powershell
    winget install -e --id Docker.DockerDesktop
    ```

    or with the installer from https://www.docker.com/products/docker-desktop/. Keep **Use WSL 2 instead of Hyper-V** checked.

2. Sign out and back in (the installer adds you to the `docker-users` group), then start **Docker Desktop** once and accept the license.

3. In Docker Desktop **Settings › General**:
    - **Use the WSL 2 based engine**: on.
    - **Start Docker Desktop when you sign in to your computer**: on. This covers machines where someone signs in. Servers need step 3 as well.

4. Check that the engine is running:

    ```powershell
    docker version            # must print both a Client and a Server section
    docker info --format "{{.OSType}}"   # linux
    ```

## 3. Start Docker automatically (Task Scheduler)

Docker Desktop is a desktop application. It starts its engine only when it runs inside a signed-in session. On a server that nobody signs in to after a reboot, the containers won't come back on their own. A scheduled task fixes that.

All containers in this guide use `restart: unless-stopped`, so they start by themselves as soon as the Docker engine is running. The task only has to start Docker Desktop.

### Option A (recommended): start Docker Desktop at sign-in of a service account, with auto-logon

This is the most reliable way, because Docker Desktop gets a real interactive session.

1. Choose the Windows account that will run Docker. Make it a member of `docker-users` and give it the right to log on locally.
2. Turn on auto-logon for that account, for example with Sysinternals **Autologon** (https://learn.microsoft.com/sysinternals/downloads/autologon), which stores the password encrypted. Lock the console afterwards or use a screen saver lock.
3. Create the task:

    ```powershell
    $action    = New-ScheduledTaskAction -Execute "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    $trigger   = New-ScheduledTaskTrigger -AtLogOn -User "$env:COMPUTERNAME\<DockerUser>"
    $settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
    $principal = New-ScheduledTaskPrincipal -UserId "$env:COMPUTERNAME\<DockerUser>" -LogonType Interactive -RunLevel Highest
    Register-ScheduledTask -TaskName "Start Docker Desktop" -Action $action -Trigger $trigger -Settings $settings -Principal $principal
    ```

### Option B: start Docker Desktop at system startup, without a sign-in

Use this when auto-logon isn't allowed. Whether Docker Desktop starts reliably without an interactive session depends on the Docker Desktop and Windows versions **(verify on your server with the reboot test below)**.

```powershell
$action    = New-ScheduledTaskAction -Execute "C:\Program Files\Docker\Docker\Docker Desktop.exe"
$trigger   = New-ScheduledTaskTrigger -AtStartup
$trigger.Delay = "PT1M"     # give Windows networking a minute
$settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
$cred      = Get-Credential "$env:COMPUTERNAME\<DockerUser>"
Register-ScheduledTask -TaskName "Start Docker Desktop" -Action $action -Trigger $trigger -Settings $settings `
    -User $cred.UserName -Password $cred.GetNetworkCredential().Password -RunLevel Highest
```

This registers the task as "Run whether user is logged on or not".

### Make sure the Docker Desktop service starts too

Set the Docker Desktop service to start automatically:

```powershell
Set-Service -Name com.docker.service -StartupType Automatic
```

### Reboot test (do this once)

1. Restart Windows.
2. Without signing in (Option B) or with auto-logon (Option A), wait 3 to 5 minutes.
3. From a remote session, or after signing in, run `docker ps`. The `contensive-redis` container (and `contensive-sql`, if used) must show `Up`.

If it doesn't, open **Task Scheduler › Start Docker Desktop › History** and check **Last Run Result**.

Contensive is safe to start before Redis is ready:
- Reads fall back to the local memory cache, if it is enabled.
- With `enableSharedRedisConnection`, a failed connection is retried after 10 seconds.
- Nothing needs restarting once Redis comes up.

## 4. Create the compose file

All Docker settings live next to Contensive's `config.json`. The examples use `D:\Contensive\`. Use the drive where your `config.json` is: Contensive looks for `D:\Contensive\config.json` first, then the first drive that has `\Contensive\config.json`.

### 4.1 Passwords in a `.env` file

Create `D:\Contensive\.env`. Docker Compose reads it automatically, so the passwords stay out of the compose file:

```ini
REDIS_PASSWORD=<redis-password>
MSSQL_SA_PASSWORD=<sql-sa-password>
```

Password rules:
- **Redis:** use letters and digits only, at least 24 characters. The same password goes into the Redis endpoint string in `config.json` (section 6). That string is split on commas, so a comma breaks it, and `=`, `$` and quotes cause escaping trouble.
- **SQL Server:** at least 8 characters, using three of these four: uppercase, lowercase, digits, symbols. Avoid `$`: Compose treats it as a variable. If you must use it, write `$$`.
- Generate a random Redis password:

    ```powershell
    -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 32 | ForEach-Object {[char]$_})
    ```

Restrict who can read the file:

```powershell
icacls D:\Contensive\.env /inheritance:r /grant:r "Administrators:F" "SYSTEM:F"
```

### 4.2 `docker-compose.yml`

Create `D:\Contensive\docker-compose.yml`:

```yaml
services:
  redis:
    image: redis:7-alpine
    container_name: contensive-redis
    restart: unless-stopped
    ports:
      - "127.0.0.1:6379:6379"          # localhost only; never publish Redis to the network
    command:
      - redis-server
      - --requirepass
      - ${REDIS_PASSWORD}
      - --appendonly
      - "yes"
      - --maxmemory
      - 512mb
      - --maxmemory-policy
      - allkeys-lru
    volumes:
      - redisdata:/data

  sqlserver:
    profiles: ["sql"]                   # optional: only starts with --profile sql
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: contensive-sql
    restart: unless-stopped
    ports:
      - "127.0.0.1:1433:1433"           # localhost only
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD}
      MSSQL_PID: "Developer"            # Developer = free, non-production. Use "Express" (free, limited) or a licensed edition for production
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  redisdata:
  sqldata:
```

Why these settings:
- **`127.0.0.1:` in `ports`** keeps both services reachable from this machine only. Contensive runs on the same machine, so it needs nothing more. Don't open ports 6379 or 1433 in Windows Firewall.
- **`--requirepass`**: Redis requires the password on every connection.
- **`--appendonly yes`** plus the `redisdata` volume keep the cache across container and server restarts.
- **`--maxmemory 512mb` with `allkeys-lru`** sets a memory limit.
  - Contensive writes some keys without an expiry, so a policy that only evicts keys with an expiry (`volatile-*`) would eventually fill Redis.
  - Eviction can also remove Contensive's invalidation keys, so size the limit so that eviction never happens in normal use (check `evicted_keys` in section 5). 512 MB is a starting point for a few sites; raise it as needed.
- **`profiles: ["sql"]`**: SQL Server is optional. `docker compose up -d` starts Redis only.
- **`sqldata` volume**: databases survive `docker compose down`. Only `docker compose down -v` deletes them.

## 5. Start and verify the containers

```powershell
cd D:\Contensive

# Redis only
docker compose up -d

# Redis and SQL Server
docker compose --profile sql up -d

docker ps --filter name=contensive
```

The `PORTS` column must show `127.0.0.1:6379->6379/tcp` (and `127.0.0.1:1433->1433/tcp`). If it shows `0.0.0.0`, fix the `ports` lines.

Check Redis. The `-a` warning about passwords on the command line is expected.

```powershell
$redisPw = (Get-Content D:\Contensive\.env | Where-Object { $_ -like 'REDIS_PASSWORD=*' }) -replace '^REDIS_PASSWORD=',''
docker exec contensive-redis redis-cli -a $redisPw ping                           # PONG
docker exec contensive-redis redis-cli -a $redisPw CONFIG GET maxmemory-policy    # allkeys-lru
docker exec contensive-redis redis-cli -a $redisPw INFO stats | Select-String "keyspace_hits|keyspace_misses|evicted_keys"
```

Check SQL Server. Allow 15 to 30 seconds after the first start:

```powershell
$sqlPw = (Get-Content D:\Contensive\.env | Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' }) -replace '^MSSQL_SA_PASSWORD=',''
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPw -C -Q "SELECT @@VERSION"
```

## 6. Point Contensive at Redis (config.json)

Contensive reads its server settings from `config.json` (for example `D:\Contensive\config.json`).
- The Redis endpoint goes in **`awsElastiCacheConfigurationEndpoint`**. Despite the name, it takes any StackExchange.Redis connection string.
- If `useSecretManager` is `true`, these settings live in the AWS secret, not in this file.

1. Back up the file:

    ```powershell
    Copy-Item D:\Contensive\config.json D:\Contensive\config.json.bak
    ```

2. Set these properties and leave the rest alone. **Every property except the last one in the file ends with a comma.**

    ```json
        "enableRemoteCache": true,
        "enableLocalMemoryCache": false,
        "enableLocalFileCache": false,
        "awsElastiCacheConfigurationEndpoint": "127.0.0.1:6379,password=<redis-password>",
    ```

    The endpoint string has the form `<host>:<port>,password=<password>`, using the same password as `REDIS_PASSWORD` in `.env`. There is no separate host, port or password setting. If you changed the published port (section 13), change the port here too.

    | Property | Value | What it does |
    |---|---|---|
    | `enableRemoteCache` | `true` | Use Redis |
    | `enableLocalMemoryCache` | `false` (recommended) or `true` | With `true`, local memory is used only when Redis misses or fails; reads still go to Redis first. `false` gives one consistent cache shared by IIS and the Task Service. |
    | `enableLocalFileCache` | `false` | Leave off |
    | `awsElastiCacheConfigurationEndpoint` | `127.0.0.1:6379,password=<redis-password>` | Redis connection string |

    Builds that include the render-speed changes (Contensive5 PR 213 and later) have three more cache flags. Missing flags use these defaults, so you only need to add them to change one:

    | Property | Default | What it does |
    |---|---|---|
    | `enableSharedRedisConnection` | `true` | One Redis connection per process, kept open (instead of connecting on every request) |
    | `enableRequestScopedCache` | `true` | Serves repeated cache reads within one page request from memory, so it doesn't ask Redis again |
    | `enableRenderProfiler` | `false` | Per-request timing in the log and, for admins, an HTML comment. It needs the NLog rule `<logger name="Contensive.RenderProfile" minlevel="info" writeTo="LogFile" final="true" />` |

3. Check that the file is still valid JSON. This prints nothing when it is valid:

    ```powershell
    Get-Content D:\Contensive\config.json -Raw | ConvertFrom-Json | Out-Null
    ```

    A missing comma stops every site at startup with `JsonReaderException: After parsing a value an unexpected character was encountered`.

4. Restart IIS **and** the Contensive Task Service. They are separate processes and both read `config.json`:

    ```powershell
    iisreset
    Restart-Service "Contensive Task Service"
    ```

5. Confirm Contensive is using Redis. Browse a site, then run:

    ```powershell
    docker exec contensive-redis redis-cli -a $redisPw DBSIZE          # greater than 0, growing
    ```

    In the Contensive log:
    - `Redis shared connection created, connected [True]` (once per process) means it worked.
    - `Redis shared connection failed` or `Exception initializing Redis connection` means a wrong password, port or a stopped container.
    - A wrong password doesn't stop the sites. They run without a working cache, so an empty `DBSIZE` while sites work means Redis is not in use.

## 7. Optional: point Contensive at SQL Server in Docker (config.json)

Start SQL Server with the `sql` profile (section 5). Then set the default data source in `config.json`:

```json
    "defaultDataSourceType": 2,
    "defaultDataSourceAddress": "localhost",
    "defaultDataSourceUsername": "sa",
    "defaultDataSourcePassword": "<sql-sa-password>",
    "defaultDataSourceSecure": false,
```

| Property | Value | Notes |
|---|---|---|
| `defaultDataSourceType` | `2` | 2 = SQL Server |
| `defaultDataSourceAddress` | `localhost` | For a non-default port, write **`localhost,1434`** with a **comma**. Contensive cuts the address at the first colon, so `localhost:1434` would silently connect to port 1433. |
| `defaultDataSourceUsername` | `sa` | Or a dedicated login you create; it needs rights to create databases if you use `cc --newapp` |
| `defaultDataSourcePassword` | the `MSSQL_SA_PASSWORD` value | Escape `\` as `\\` and `"` as `\"` in JSON |
| `defaultDataSourceSecure` | `false` | The container has no trusted TLS certificate. `true` adds `Encrypt=yes`, which fails against it |

Contensive builds this connection string from those values. Each site's database name is added per app:

```text
server=localhost;User Id=sa;Password=<sql-sa-password>;Encrypt=false;Database=<appName>;
```

Validate the JSON and restart IIS and the Task Service, as in section 6, steps 3 and 4.

## 8. Daily operations

```powershell
cd D:\Contensive

docker compose up -d                       # start Redis (add --profile sql to include SQL Server)
docker compose --profile sql down          # stop everything; data is kept (never add -v unless you want it deleted)
docker ps -a --filter name=contensive      # status
docker logs contensive-redis               # logs
docker logs contensive-sql
docker compose pull                        # newer images; then 'docker compose --profile sql up -d' to recreate
```

With `restart: unless-stopped`, the containers come back after a reboot, as long as Docker Desktop starts (section 3). A container you stopped yourself with `docker stop` stays stopped until you start it again.

## 9. Working with sites

These steps apply when SQL Server runs in Docker.

```powershell
cc --newapp mySiteName      # creates the database in the container, config entry, folders, IIS site
cc --status                 # list configured apps
cc --delete mySiteName      # remove the app and drop its database
cc --iisreset               # recycle IIS (for example after the container restarted)
```

All sites share the one SQL Server container. Each site has its own database, named after the app.

List the databases:

```powershell
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPw -C -Q "SELECT name FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb')"
```

To create only the database, then build its schema:

```powershell
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPw -C -Q "CREATE DATABASE [mySiteName]"
cc --upgrade mySiteName
```

## 10. Site snapshots (database)

A site has two pieces of state: its database and its files under `D:\inetpub\<appName>\`. Back up the files as usual. For the database:

```powershell
# save
docker exec contensive-sql mkdir -p /var/opt/mssql/backup
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPw -C -Q "BACKUP DATABASE [mySiteName] TO DISK = '/var/opt/mssql/backup/mySiteName.bak' WITH INIT"
docker cp contensive-sql:/var/opt/mssql/backup/mySiteName.bak D:\Contensive\backups\mySiteName.bak

# restore (also into a fresh container: start it first and wait ~30 seconds)
docker cp D:\Contensive\backups\mySiteName.bak contensive-sql:/var/opt/mssql/backup/mySiteName.bak
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $sqlPw -C -Q "RESTORE DATABASE [mySiteName] FROM DISK = '/var/opt/mssql/backup/mySiteName.bak' WITH REPLACE"
```

Restoring a snapshot is much faster than `cc --newapp`, because it skips the schema build. Keep backups in `D:\Contensive\backups\`, and commit only small test databases to a repo.

## 11. Switching between Docker and cloud services

Keep a copy of each `config.json` variant:
- `config.json.bak` holds the original.
- To switch, edit the properties from sections 6 and 7, validate the JSON, and restart IIS and the Task Service.

To go back to memory-only cache:

```json
    "enableRemoteCache": false,
    "enableLocalMemoryCache": true,
    "awsElastiCacheConfigurationEndpoint": "",
```

## 12. GitHub Actions CI

In CI the containers are throwaway and unreachable from outside the job, so Redis can run without a password there. On any persistent server, use the password setup above.

```yaml
name: Test
on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports: ["1433:1433"]
        env:
          ACCEPT_EULA: Y
          MSSQL_SA_PASSWORD: ${{ secrets.CI_SQL_SA_PASSWORD }}
      redis:
        image: redis:7-alpine
        ports: ["6379:6379"]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Create Contensive config
        shell: pwsh
        env:
          SQL_PW: ${{ secrets.CI_SQL_SA_PASSWORD }}
        run: |
          New-Item -ItemType Directory -Force -Path "D:\Contensive" | Out-Null
          @{
            defaultDataSourceType = 2
            defaultDataSourceAddress = "localhost"
            defaultDataSourceUsername = "sa"
            defaultDataSourcePassword = $env:SQL_PW
            defaultDataSourceSecure = $false
            isLocalFileSystem = $true
            localDataDriveLetter = "D"
            name = "ci"
            enableLocalMemoryCache = $false
            enableLocalFileCache = $false
            enableRemoteCache = $true
            awsElastiCacheConfigurationEndpoint = "127.0.0.1:6379"
            productionEnvironment = $false
            programFilesPath = "C:\Program Files\Contensive\Cli\"
            allowTaskRunnerService = $false
            allowTaskSchedulerService = $false
            apps = @{}
          } | ConvertTo-Json | Set-Content "D:\Contensive\config.json"
      - name: Create test site
        shell: pwsh
        run: |
          Start-Sleep -Seconds 20
          cc --newapp testApp
      - name: Build and test
        run: dotnet test your-test-project.csproj
```

**Limitation:** GitHub Actions `services:` containers run only on Linux runners, so this example as written needs a self-hosted Windows runner that has Docker Desktop installed and the runner label in `runs-on`. On GitHub-hosted `windows-latest` the services can't start. There, either point the config at hosted SQL Server and Redis instances, or install them on the runner in a setup step.

## 13. Troubleshooting

| Symptom | Cause and fix |
|---|---|
| Every site fails at start with `JsonReaderException: After parsing a value an unexpected character was encountered … Path '<property>'` | A comma is missing after the named property in `config.json`. Fix it and validate (section 6, step 3). |
| Sites work, but `DBSIZE` stays 0 | Contensive isn't using Redis. Check that `enableRemoteCache` is `true`, the endpoint string is right, and you restarted IIS. Look in the log for `Redis shared connection failed` / `Exception initializing Redis connection` / `NOAUTH`. |
| `NOAUTH Authentication required` or `WRONGPASS` | The password in the endpoint string doesn't match `REDIS_PASSWORD`. Changing `.env` needs `docker compose up -d` to recreate the container. |
| `docker ps` is empty after a reboot | Docker Desktop didn't start. Check the scheduled task (section 3) and `Get-Service com.docker.service`. |
| `docker version` shows no Server section | The Docker engine isn't running. Start Docker Desktop. If it complains about WSL, run `wsl --update` and check virtualization (Prerequisites). |
| `evicted_keys` grows | Redis is full. Raise `--maxmemory` in the compose file and run `docker compose up -d`. |
| Port 6379 or 1433 is already in use (`netstat -ano \| findstr :6379`) | Another Redis or SQL Server is installed. Stop it, or publish a different host port, for example `"127.0.0.1:6380:6379"`. Then change the endpoint to `127.0.0.1:6380,password=…`, or for SQL to `"127.0.0.1:1434:1433"` and `localhost,1434` (comma). |
| SQL `Login failed for user 'sa'` | The password doesn't meet the complexity rules (the container log says so), or `.env` changed after the first start. The sa password is set only when the volume is created. Change it inside the container: `docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<old>" -C -Q "ALTER LOGIN sa WITH PASSWORD = '<new>'"` |
| SQL connection refused right after start | SQL Server needs 15 to 30 seconds to initialize. Wait and retry. |
| Databases missing after restart | Someone ran `docker compose down -v`, which deleted the `sqldata` volume. Restore from a snapshot (section 10) or recreate with `cc --newapp`. |

# Docker Development Services Setup Guide

Replace cloud-hosted SQL Server and Redis with local Docker containers for development and staging. No code changes required — only the Contensive config file changes.

## Prerequisites

- Docker Desktop installed and running on Windows
- Contensive CLI (`cc`) installed at `C:\Program Files\Contensive\Cli\`

## One-Time Setup

### 1. Create the Docker Compose file

Create `D:\Contensive\docker-compose.yml`:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: contensive-sql
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "<YourStrongPassword>"
    volumes:
      - sqldata:/var/opt/mssql

  redis:
    image: redis:7-alpine
    container_name: contensive-redis
    ports:
      - "6379:6379"

volumes:
  sqldata:
```

Replace `<YourStrongPassword>` with a password that meets SQL Server complexity requirements (at least 8 characters, mix of uppercase, lowercase, numbers, and/or symbols).

The `sqldata` volume persists database files across container restarts. Your databases survive `docker compose down` and only get deleted if you explicitly run `docker compose down -v`. Redis runs in-memory with no persistence — it starts empty each time, which is fine since Contensive treats it as a cache.

### 2. Start the containers

```powershell
cd D:\Contensive
docker compose up -d
```

Verify it's running:

```powershell
docker ps
```

You should see `contensive-sql` listed with port `0.0.0.0:1433->1433/tcp` and `contensive-redis` with port `0.0.0.0:6379->6379/tcp`.

### 3. Update config.json

Edit `D:\Contensive\config.json`. Change these fields:

**SQL Server:**

| Field | New value (Docker) |
|---|---|
| `defaultDataSourceAddress` | `localhost` |
| `defaultDataSourceUsername` | `sa` |
| `defaultDataSourcePassword` | `<YourStrongPassword>` |

The `defaultDataSourceSecure` field should remain `false` — Docker SQL Server doesn't use TLS by default.

**Redis cache:**

| Field | New value (Docker) |
|---|---|
| `enableRemoteCache` | `true` |
| `awsElastiCacheConfigurationEndpoint` | `127.0.0.1:6379` |

With Redis running locally, cache operations have sub-millisecond latency.

**Keep a backup of your original config.json before editing**, so you can switch back at any time:

```powershell
copy D:\Contensive\config.json D:\Contensive\config.json.backup
```

### 4. Create a site database

```powershell
cc --newapp mySiteName
```

This will:
- Create a new database named `mySiteName` on the Docker SQL Server
- Add the app entry to `D:\Contensive\config.json`
- Create local folders at `d:\inetpub\mySiteName\`
- Build the database schema from Contensive metadata
- Set up the IIS site and deploy WebApi

Alternatively, create just the database manually and run an upgrade to build the schema:

```powershell
# Connect to Docker SQL Server and create the database
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<YourStrongPassword>" -C -Q "CREATE DATABASE [mySiteName]"

# Then run the Contensive upgrade to build schema from metadata
cc --upgrade mySiteName
```

## Daily Operations

### Start the containers

```powershell
cd D:\Contensive
docker compose up -d
```

Or start them from Docker Desktop. This starts both SQL Server and Redis.

All databases you previously created are still there — the `sqldata` volume persists data across restarts. Redis starts empty each time (it's a cache, so this is expected).

### Stop the containers

```powershell
cd D:\Contensive
docker compose down
```

This stops both containers but preserves SQL Server data. Databases will be there when you start again.

### Check container status

```powershell
docker ps -a --filter name=contensive
```

### View logs

```powershell
# SQL Server logs
docker logs contensive-sql

# Redis logs
docker logs contensive-redis
```

## Working with Sites

### Create a new site

1. Make sure the Docker containers are running (`docker compose up -d`).

2. Run the Contensive CLI:

```powershell
cc --newapp mySiteName
```

3. The site is now accessible at the domain configured during setup.

### Start an existing site

Sites are available whenever the Docker SQL Server container is running. There is no per-site start/stop — all databases live in the same SQL Server instance.

```powershell
# Start the container (all sites become available)
cd D:\Contensive
docker compose up -d

# Verify the database exists
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<YourStrongPassword>" -C -Q "SELECT name FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb')"
```

If IIS needs to be recycled after the container starts:

```powershell
cc --iisreset
```

### Run multiple sites at the same time

All sites share the same Docker SQL Server container. Each site has its own database (named after the app). They all run simultaneously with no extra steps.

```powershell
# Create multiple sites
cc --newapp siteA
cc --newapp siteB
cc --newapp siteC

# All three databases exist in the same container
# All three sites are accessible through IIS simultaneously
```

To see which databases exist:

```powershell
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<YourStrongPassword>" -C -Q "SELECT name FROM sys.databases WHERE name NOT IN ('master','tempdb','model','msdb')"
```

To see which apps are configured in Contensive:

```powershell
cc --status
```

### Delete a site

```powershell
cc --delete mySiteName
```

This removes the app from config.json and drops the database.

## Site Snapshots (Database + Files)

A Contensive site has two pieces of state: the SQL Server database and the filesystem under `d:\inetpub\<appName>\`. You can save and restore both together using snapshot scripts.

### Save a snapshot (database only, manual)

```powershell
# Back up
docker exec contensive-sql mkdir -p /var/opt/mssql/backup
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<YourStrongPassword>" -C -Q "BACKUP DATABASE [mySiteName] TO DISK = '/var/opt/mssql/backup/mySiteName.bak' WITH INIT"
docker cp contensive-sql:/var/opt/mssql/backup/mySiteName.bak D:\Contensive\backups\mySiteName.bak
```

### Restore a snapshot (database only, manual)

```powershell
docker cp D:\Contensive\backups\mySiteName.bak contensive-sql:/var/opt/mssql/backup/mySiteName.bak
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<YourStrongPassword>" -C -Q "RESTORE DATABASE [mySiteName] FROM DISK = '/var/opt/mssql/backup/mySiteName.bak' WITH REPLACE"
```

### Restore into a fresh container

If the Docker volume was deleted or you're setting up a new machine, start the container first, then restore:

```powershell
cd D:\Contensive
docker compose up -d

# Wait for SQL Server to initialize
Start-Sleep -Seconds 15

# Restore from a previously saved snapshot
docker cp D:\Contensive\backups\mySiteName.bak contensive-sql:/var/opt/mssql/backup/mySiteName.bak
docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<YourStrongPassword>" -C -Q "RESTORE DATABASE [mySiteName] FROM DISK = '/var/opt/mssql/backup/mySiteName.bak' WITH REPLACE"
```

This is much faster than `cc --newapp` because it skips the full schema build.

### Where to store backups

| Location | Use case |
|---|---|
| `D:\Contensive\backups\` | Local dev machine — quick access for daily resets |
| Git repo (e.g., `tests/fixtures/`) | Share with team — but only for small test databases |

## Switching Between Docker and Cloud

### Switch to Docker (local)

Edit `D:\Contensive\config.json`:

```json
{
  "defaultDataSourceAddress": "localhost",
  "defaultDataSourceUsername": "sa",
  "defaultDataSourcePassword": "<YourStrongPassword>",
  "enableRemoteCache": true,
  "awsElastiCacheConfigurationEndpoint": "127.0.0.1:6379"
}
```

### Switch back to cloud

Restore your backup config or edit `D:\Contensive\config.json` with your cloud provider's connection details.

The switch is instant — no code changes, no rebuilds. Just edit the fields and restart IIS (`cc --iisreset`).

## GitHub Actions CI

To run tests in GitHub Actions with disposable SQL Server and Redis containers:

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
          - 1433:1433
        env:
          ACCEPT_EULA: Y
          MSSQL_SA_PASSWORD: "<CI-StrongPassword>"

      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Create Contensive config
        shell: powershell
        run: |
          New-Item -ItemType Directory -Force -Path "D:\Contensive"
          @'
          {
            "defaultDataSourceAddress": "localhost",
            "defaultDataSourceUsername": "sa",
            "defaultDataSourcePassword": "<CI-StrongPassword>",
            "defaultDataSourceSecure": false,
            "defaultDataSourceType": 2,
            "isLocalFileSystem": true,
            "localDataDriveLetter": "d",
            "name": "ci",
            "enableLocalMemoryCache": true,
            "enableLocalFileCache": false,
            "enableRemoteCache": true,
            "awsElastiCacheConfigurationEndpoint": "127.0.0.1:6379",
            "productionEnvironment": false,
            "programFilesPath": "C:\\Program Files\\Contensive\\Cli\\",
            "allowTaskRunnerService": false,
            "allowTaskSchedulerService": false,
            "apps": {}
          }
          '@ | Set-Content "D:\Contensive\config.json"

      - name: Create test database
        shell: powershell
        run: |
          Start-Sleep -Seconds 15
          sqlcmd -S localhost -U sa -P "<CI-StrongPassword>" -Q "CREATE DATABASE [testApp]"

      - name: Install Contensive CLI and create site
        shell: powershell
        run: |
          cc --newapp testApp

      - name: Build and test
        run: dotnet test your-test-project.csproj
```

Both containers start fresh for each CI run. No persistent state, no cloud costs, no shared database conflicts.

## Troubleshooting

### Container won't start

Check if port 1433 is already in use (e.g., a local SQL Server installation):

```powershell
netstat -ano | findstr :1433
```

If something is using port 1433, either stop that service or change the Docker port mapping in `docker-compose.yml`:

```yaml
ports:
  - "1434:1433"  # Use port 1434 on host
```

Then update `defaultDataSourceAddress` in config.json to `localhost,1434` (SQL Server uses comma for port, not colon).

### Connection refused

Make sure the container is running:

```powershell
docker ps --filter name=contensive-sql
```

SQL Server takes 10-15 seconds to initialize after the container starts. Wait and retry.

### Password rejected

The `MSSQL_SA_PASSWORD` must meet SQL Server complexity requirements:
- At least 8 characters
- Mix of uppercase, lowercase, numbers, and/or symbols

If you change the password in `docker-compose.yml`, you need to either:
- Delete the volume and recreate: `docker compose down -v && docker compose up -d`
- Or change it inside the running container: `docker exec contensive-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<OldPassword>" -C -Q "ALTER LOGIN sa WITH PASSWORD = '<NewPassword>'"`

### Database missing after restart

If you ran `docker compose down -v` (with the `-v` flag), the data volume was deleted. Recreate the databases:

```powershell
cc --newapp mySiteName
```

Without the `-v` flag, `docker compose down` preserves data.

### Redis connection refused

Make sure the Redis container is running:

```powershell
docker ps --filter name=contensive-redis
```

If port 6379 is already in use (e.g., a local Redis installation), change the port mapping in `docker-compose.yml`:

```yaml
ports:
  - "6380:6379"  # Use port 6380 on host
```

Then update `awsElastiCacheConfigurationEndpoint` in config.json to `127.0.0.1:6380`.

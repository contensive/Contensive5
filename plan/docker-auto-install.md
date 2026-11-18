# Docker Auto-Install for SQL Server and Redis

## Overview

Extend the Contensive CLI `cc --configure` command to optionally install and manage Docker containers for SQL Server and Redis. The user runs `cc --configure` interactively, selects Docker-managed services, and the CLI handles container lifecycle — pulling images, starting containers, waiting for readiness, and writing the resulting endpoints into `config.json`.

## Phase 1 — Interactive Docker Setup via `cc --configure`

The user copies the Contensive zip to a new server, unzips and installs it, then runs `cc --configure` from an elevated command prompt. The configure flow gains new prompts for Docker-managed SQL Server and Redis. When the user completes configuration, the CLI sets up Docker and waits for services before saving `config.json`. The user then runs `cc --n appname domain` as usual.

### User Flow

```
cc --configure
  ... existing prompts (server name, email, production, AWS, filesystem, etc.) ...

  Docker Services
  ───────────────
  Contensive can install SQL Server and Redis as Docker containers on this machine.
  Docker Desktop must be installed and running.

  Use Docker to manage SQL Server (y/n)? [n]
    → (y) SA Password: [generated or entered]
    → (y) Host Port: [1433]
  Use Docker to manage Redis (y/n)? [n]
    → (y) Host Port: [6379]

  ... existing SQL endpoint prompt is SKIPPED if Docker SQL was selected ...
  ... existing cache prompt is SKIPPED if Docker Redis was selected ...

  Starting Docker services...
    ✓ Docker is available (Docker Desktop 4.x.x)
    ✓ SQL Server container started (contensive-sql, port 1433)
    ✓ Waiting for SQL Server... ready (12s)
    ✓ Redis container started (contensive-redis, port 6379)
    ✓ Redis connection verified

  Configuration saved.
```

### Implementation Steps

#### Step 1: Add Docker properties to ServerConfigBaseModel

Add new abstract properties to `source/CPBase/BaseModels/ServerConfigBaseModel.cs`:

```csharp
/// <summary>
/// If true, Contensive manages a Docker container for SQL Server on this machine.
/// The CLI will start/verify the container during --configure and write the
/// connection details (localhost:{port}, sa, password) into config automatically.
/// </summary>
public abstract bool enableDockerSqlServer { get; set; }

/// <summary>
/// Host port mapped to the Docker SQL Server container's port 1433.
/// Default 1433. Change if a local SQL Server instance already occupies that port.
/// </summary>
public abstract int dockerSqlServerPort { get; set; }

/// <summary>
/// If true, Contensive manages a Docker container for Redis on this machine.
/// The CLI will start/verify the container during --configure and write the
/// cache endpoint (127.0.0.1:{port}) into config automatically.
/// </summary>
public abstract bool enableDockerRedis { get; set; }

/// <summary>
/// Host port mapped to the Docker Redis container's port 6379.
/// Default 6379. Change if something already occupies that port.
/// </summary>
public abstract int dockerRedisPort { get; set; }
```

Add matching concrete properties to `ServerConfigModel` in the Processor project.

#### Step 2: Create DockerController

Create `source/Cli/Controllers/DockerController.cs` — a static helper that wraps Docker CLI commands. All Docker operations go through this single controller.

Methods:

- `isDockerAvailable()` — Runs `docker info` and returns `(bool success, string version)`. If Docker is not installed or the daemon is not running, returns false with a descriptive message.
- `ensureContainer(string containerName, string image, Dictionary<string,string> envVars, int hostPort, int containerPort, string volumeMapping)` — Checks if the named container exists and is running. If it does not exist, runs `docker run -d`. If it exists but is stopped, runs `docker start`. Returns `(bool success, string errorMessage)`.
- `waitForSqlServer(string host, int port, string user, string password, int timeoutSeconds)` — Polls the SQL Server endpoint using `DbServerController.getSqlOpenErrors()` in a retry loop (2-second intervals, configurable timeout defaulting to 60 seconds). Returns `(bool ready, int elapsedSeconds, string errorMessage)`.
- `waitForRedis(string endpoint, int timeoutSeconds)` — Polls Redis using `CacheController.testConnection()` in a retry loop. Returns `(bool ready, string errorMessage)`.
- `isPortInUse(int port)` — Checks if a TCP port is already bound on localhost. Used to warn the user before attempting to start a container on an occupied port.

All methods invoke `docker` via `System.Diagnostics.Process` with output/error capture. No Docker SDK dependency — the Docker CLI is the only requirement.

#### Step 3: Modify ConfigureCmd to add Docker prompts

In `source/Cli/Views/ConfigureCmd.cs`, insert a new Docker Services section between the filesystem/CloudWatch prompts and the SQL Server endpoint prompt. The logic:

1. **Docker availability check** — Before prompting, call `DockerController.isDockerAvailable()`. If Docker is not available, skip Docker prompts entirely and fall through to the existing manual SQL/cache configuration. Display: `"Docker not detected. Skipping Docker service options. Install Docker Desktop to enable this feature."`

2. **Docker SQL Server prompt**:
   - `"Use Docker to manage SQL Server (y/n)?"` — Default `n` (or `y` if `enableDockerSqlServer` is already true in existing config).
   - If yes:
     - Prompt for SA password. Default: use existing `defaultDataSourcePassword` if set, otherwise generate a compliant password (16 chars, mixed case + digits + symbol) and display it.
     - Prompt for host port. Default: `1433`. If `isPortInUse(1433)` returns true, warn the user and suggest `1434`.
     - Set `enableDockerSqlServer = true`, `dockerSqlServerPort = {port}`.

3. **Docker Redis prompt**:
   - `"Use Docker to manage Redis (y/n)?"` — Default `n` (or `y` if `enableDockerRedis` is already true).
   - If yes:
     - Prompt for host port. Default: `6379`. Warn if in use.
     - Set `enableDockerRedis = true`, `dockerRedisPort = {port}`.

4. **Skip existing prompts when Docker is selected**:
   - If `enableDockerSqlServer` is true, skip the SQL Server endpoint/credentials prompt block. Instead, auto-set:
     - `secrets.defaultDataSourceAddress = $"localhost,{dockerSqlServerPort}"` (SQL Server uses comma for port)
     - `secrets.defaultDataSourceUsername = "sa"`
     - `secrets.defaultDataSourcePassword = {the SA password}`
     - `serverConfig.defaultDataSourceSecure = false`
   - If `enableDockerRedis` is true, skip the cache service prompt block. Instead, auto-set:
     - `serverConfig.enableRemoteCache = true`
     - `serverConfig.enableLocalMemoryCache = false`
     - `serverConfig.enableLocalFileCache = false`
     - `serverConfig.awsElastiCacheConfigurationEndpoint = $"127.0.0.1:{dockerRedisPort}"`

5. **Start Docker services** — After all prompts are collected but before the final `serverConfig.save()`:
   - If `enableDockerSqlServer`:
     - Call `ensureContainer("contensive-sql", "mcr.microsoft.com/mssql/server:2022-latest", envVars, port, 1433, "contensive-sqldata:/var/opt/mssql")`
     - Call `waitForSqlServer()` — display a progress message with elapsed seconds
     - On failure: display error, offer to retry or abort
   - If `enableDockerRedis`:
     - Call `ensureContainer("contensive-redis", "redis:7-alpine", null, port, 6379, null)`
     - Call `waitForRedis()` — verify connection
     - On failure: display error, offer to fall back to local memory cache

6. **Verify connections** — Run the same `DbServerController.getSqlOpenErrors()` and `CacheController.testConnection()` that the existing configure flow already uses. This confirms the Docker-started services are reachable through the configured endpoints.

#### Step 4: Add docker-compose.yml generation (optional convenience)

After successfully starting both containers, offer to write a `docker-compose.yml` to the Contensive data folder (`{drive}:\Contensive\docker-compose.yml`). This file would match the structure in the existing `etc/Docker/Docker-Setup.md` guide, populated with the user's chosen ports and password. This is a convenience for the user to manage the containers with `docker compose` commands later, but is not required for the CLI-managed containers to function.

#### Step 5: Update StatusCmd to show Docker container status

In `source/Cli/Views/StatusCmd.cs`, when `enableDockerSqlServer` or `enableDockerRedis` is true, add a Docker Services section to the status output:

```
Docker Services:
  SQL Server: running (contensive-sql, port 1433, uptime 3d 2h)
  Redis:      running (contensive-redis, port 6379, uptime 3d 2h)
```

Use `docker inspect --format` to get container state and start time.

#### Step 6: Handle Docker container restart on `cc --configure` re-run

When the user re-runs `cc --configure` on a server already configured with Docker services:

- If Docker SQL was enabled and the container exists and is running, skip the start step. Display: `"Docker SQL Server is already running (contensive-sql)."`
- If the container exists but is stopped, start it and wait for readiness.
- If the user changes from Docker to non-Docker (answers `n`), stop and remove the container. Set `enableDockerSqlServer = false`. Prompt for manual SQL Server credentials as normal.
- Same logic for Redis.

#### Step 7: Update help text

Update help text in `ConfigureCmd.helpText` and `HelpCmd` to mention Docker service management. Update `etc/Docker/Docker-Setup.md` to reference the new `--configure` integration as an alternative to manual `docker-compose.yml` setup.

### Files Changed (Phase 1)

| File | Change |
|------|--------|
| `source/CPBase/BaseModels/ServerConfigBaseModel.cs` | Add 4 abstract properties |
| `source/Processor/Models/Domain/ServerConfigModel.cs` | Add 4 concrete properties with defaults |
| `source/Cli/Controllers/DockerController.cs` | **New file** — Docker CLI wrapper |
| `source/Cli/Views/ConfigureCmd.cs` | Add Docker prompts, conditional skip of SQL/cache prompts, container startup |
| `source/Cli/Views/StatusCmd.cs` | Add Docker container status display |
| `source/Cli/Views/HelpCmd.cs` | Update help text |
| `etc/Docker/Docker-Setup.md` | Reference `--configure` as alternative setup path |

### Considerations

- **Elevated permissions** — `cc --configure` currently does not require elevation, but `docker run` with volume mounts may need it depending on Docker Desktop configuration. The Docker prompts should check and warn if elevation is needed.
- **Docker Desktop vs Docker Engine** — On Windows, Docker Desktop is the standard. The `docker info` check handles both. No need to install Docker programmatically — if it is not present, skip Docker options and tell the user to install it.
- **SQL Server password complexity** — SQL Server 2022 requires at least 8 characters with mixed case, digits, and symbols. The auto-generated password must meet this. Validate user-entered passwords against the same rules before attempting container creation.
- **Named volumes** — Use `contensive-sqldata` as the Docker volume name for SQL Server data persistence. This survives `docker stop`/`docker start` and `docker rm` (unless `-v` is used). Redis runs without persistence (cache-only).
- **Port conflicts** — The `isPortInUse()` check prevents the most common failure mode. If a local SQL Server Express is on 1433, suggest 1434. Display clear instructions if the port check fails.
- **Container naming** — Use fixed names (`contensive-sql`, `contensive-redis`) matching the existing Docker guide. This avoids creating duplicate containers on re-runs.

---

## Phase 2 — Non-Interactive / Argument-Driven Configuration (Future)

Phase 2 adds command-line arguments to `cc --configure` so the entire setup can be scripted without interactive prompts. This enables automated provisioning, CI pipelines, and infrastructure-as-code scenarios.

### Concept

```bash
# Full non-interactive setup with Docker SQL + Redis
cc --configure --serverName=prod1 --email=ops@example.com --production=y --dockerSql=y --dockerSqlPassword=MyStr0ng!Pass --dockerRedis=y --localDrive=d

# Docker SQL only, manual Redis
cc --configure --dockerSql=y --dockerSqlPassword=MyStr0ng!Pass --cacheMode=local

# No Docker, external SQL + Redis (cloud deployment)
cc --configure --sqlServer=rds-endpoint.amazonaws.com --sqlUser=admin --sqlPassword=xxx --cacheMode=redis --redisEndpoint=elasticache-endpoint:6379

# Then create the app non-interactively
cc --n myapp www.example.com
```

### Scope

- Parse `--key=value` arguments from `args[]` in `ConfigureCmd`
- When all required values are supplied via arguments, skip all interactive prompts
- When some values are supplied, use them as defaults and prompt only for missing values
- Same Docker container lifecycle as Phase 1 (just driven by arguments instead of prompts)
- Enable full CI/CD automation: `cc --configure ... && cc --n appname domain`
- Support environment variable fallbacks for sensitive values (passwords, AWS keys) so they do not appear in command history

### Dependencies

- Phase 1 must be complete (DockerController, ServerConfigModel properties, container lifecycle logic)
- Argument parsing infrastructure in `mainClass.cs` (extend existing `getNextCmdArg` or add a key-value parser)

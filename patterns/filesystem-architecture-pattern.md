# Filesystem Architecture Pattern

> All patterns and API reference: [Patterns Index](index.md)

> Source files: `source/Processor/Controllers/FileController.cs`, `source/Processor/Controllers/CoreController.cs`, `source/Processor/Models/Domain/ServerConfigModel.cs`

> API reference: [Filesystem & Utilities API](api-filesystem-utilities-reference.md)

---

## Overview

Contensive provides five filesystem instances. Each application has access to all five, but they differ in scope, visibility, and whether they support remote (S3) storage.

| Filesystem | API Instance | Purpose | Public | Shared Across Nodes | Supports Remote (S3) |
|---|---|---|---|---|---|
| **CDN Files** | `cp.CdnFiles` | Content uploads, images, file fields. Served via CDN URL. | Yes | Yes | Yes |
| **WWW Files** | `cp.WwwFiles` | Web root files served by the webserver (CSS, JS, images). | Yes | Yes | Yes |
| **Private Files** | `cp.PrivateFiles` | Server-side storage not accessible via HTTP. | No | Yes | Yes |
| **Temp Files** | `cp.TempFiles` | Temporary working files, auto-deleted periodically. | No | No | No — always local |
| **Program Data** | (internal) | System config files, logs. Location of `config.json`. | No | No | No — always local |

All five share the same `CPFileSystemBaseClass` API for reads, writes, copies, and listings. See [Filesystem & Utilities API](api-filesystem-utilities-reference.md) for the full method reference.

---

## Local File Mode (`isLocalFileSystem=true`)

This is the default. All reads and writes for every filesystem go directly to the local disk. No AWS S3 interaction occurs.

**When to use:** Single-server deployments where one server is the only consumer of the files.

**Behavior:**
- `Read` / `ReadBinary` — reads from local disk
- `Save` / `SaveBinary` — writes to local disk
- `Append` — appends to local file
- `Delete` — deletes from local disk
- `FileExists` — checks local disk
- `CopyLocalToRemote` / `CopyRemoteToLocal` — no-op (returns false)

Local paths are constructed from the `localDataDriveLetter` config value and the application name. For example, CDN files default to `{drive}:\inetpub\{appName}\files\`.

---

## Remote File Mode (`isLocalFileSystem=false`)

When remote mode is enabled, CDN files, WWW files, and private files use AWS S3 as the authoritative store. The local filesystem acts as a mirror/cache. Temp files and program data files are unaffected — they remain local-only.

**When to use:** Multi-server deployments behind a load balancer, or any deployment where files must be shared across servers.

### Read Path

When `readFileText` or `readFileBinary` is called:

1. Call `copyFileRemoteToLocal(pathFilename)`, which checks `localFileStale()`
2. If the local file is stale (missing, unreadable, or remote is newer/different size), delete the local copy and download from S3
3. If the remote file does not exist but a local copy exists, the local copy is kept (staleness check returns "not stale")
4. If `copyFileRemoteToLocal` returns false (remote file does not exist and local was deleted), the read returns empty
5. Read from local disk and return the content

### Staleness Check (`localFileStale`)

The staleness check determines whether the local mirror needs to be refreshed from S3:

| Condition | Result |
|---|---|
| `isLocalFileSystem=true` | Never stale (always returns false) |
| Local file does not exist | Stale — needs download |
| Local file details unreadable | Stale — needs download |
| Remote file does not exist | Not stale — local copy is kept |
| Remote file has different size or newer date | Stale — needs re-download |
| Same size and local is same age or newer | Not stale — skip download |

### Write Path

When `Save` or `SaveBinary` is called:

1. Write to local disk first
2. Call `copyFileLocalToRemote(pathFilename)` to upload to S3
3. After upload, sync the local file's `LastWriteTime` to match the S3 object's last-modified date (this keeps future staleness checks accurate)

### Append Path

1. Call `copyFileRemoteToLocal` to get the latest version from S3
2. Append to the local file
3. Call `copyFileLocalToRemote` to upload the merged result back to S3

### Delete Path

1. Delete from S3 (`deleteFile_remote`)
2. Delete the local mirror (`deleteFile_local`)

### S3 Key Structure

Each filesystem maps to a prefix in the S3 bucket:

| Filesystem | Default S3 Prefix |
|---|---|
| CDN Files | `/{appName}/files/` |
| WWW Files | `/{appName}/www/` |
| Private Files | `/{appName}/private/` |

These defaults can be overridden in the app config (`remoteFilePath`, `remoteWwwPath`, `remotePrivatePath`).

---

## Multi-Server / Load-Balanced Deployments

When all three stateful services are remote, multiple front-end servers can operate behind a load balancer with no server holding authoritative data locally:

| Service | Local Mode | Remote Mode |
|---|---|---|
| **Files** | `isLocalFileSystem=true` | `isLocalFileSystem=false` — S3 is the source of truth |
| **Database** | Local SQL Server | Remote SQL Server — shared connection string |
| **Cache** | `enableLocalMemoryCache=true` | `enableRemoteCache=true` — shared Redis instance |

### Prerequisites for Scale-Out

1. **`isLocalFileSystem=false`** — all content files stored in a shared S3 bucket
2. **Remote SQL Server** — all servers share the same database connection string
3. **Remote Redis cache** (`enableRemoteCache=true`) — all servers share the same cache. See [Redis Cache Setup](redis-cache-setup.md) for configuration details.
4. **AWS credentials and S3 bucket** — `awsBucketName`, `awsRegionName`, and either IAM role credentials or `awsAccessKey`/`awsSecretAccessKey` configured in `config.json` or AWS Secrets Manager
5. **`enableLocalMemoryCache=false`** and **`enableLocalFileCache=false`** — local caches must be disabled to prevent stale reads across servers

### Why This Works

Each front-end server's local filesystem is a disposable cache. When a server reads a file, it checks S3 for the latest version. When it writes, the file is pushed to S3 immediately. Any server can be rebuilt, replaced, or added without data loss because S3, the database, and Redis hold all shared state.

Temp files remain local because they are transient working files that do not need to be shared.

---

## Migration: Switching Between Modes

**The current code does not automatically migrate files when `isLocalFileSystem` is changed.** Switching the flag assumes files are already in the correct location. Without a manual migration step, files may become inaccessible.

### Local → Remote (`isLocalFileSystem` true → false)

After switching to remote mode:
- **Files that exist locally but not in S3 will still be readable** on the server where they exist. The staleness check treats a missing remote file as "not stale," so the local copy is preserved and returned.
- **These files are never pushed to S3 by reads.** The `copyFileLocalToRemote` method is only called during writes. Other servers in a multi-node setup will not have access to these files.
- **Risk:** In a load-balanced environment, requests routed to a different server will not find the file.

**Required migration step:** Before switching to remote mode, copy all local files to S3:

```csharp
// API — call for each filesystem
cp.CdnFiles.CopyLocalToRemote("path/to/file.ext");

// Or use the internal bulk method to copy entire directory trees
// (available within the Processor project)
core.cdnFiles.copyPathLocalToRemote("");
core.wwwFiles.copyPathLocalToRemote("");
core.privateFiles.copyPathLocalToRemote("");
```

### Remote → Local (`isLocalFileSystem` false → true)

After switching to local mode:
- **All remote (S3) operations are skipped.** The read path checks `if (!isLocalFileSystem)` and bypasses the entire remote sync block.
- **Files that exist only in S3 will return empty.** There is no fallback to check S3 when `isLocalFileSystem=true`.
- **Files that were previously cached locally may still be readable** if the local mirror copy was not deleted, but this is not guaranteed.

**Required migration step:** Before switching to local mode, copy all remote files to local storage:

```csharp
// API — call for each filesystem
cp.CdnFiles.CopyRemoteToLocal("path/to/file.ext");

// Or use the internal bulk method
core.cdnFiles.copyPathRemoteToLocal("");
core.wwwFiles.copyPathRemoteToLocal("");
core.privateFiles.copyPathRemoteToLocal("");
```

### Migration Checklist

1. **Plan the migration during a maintenance window.** File sync can take significant time for large sites.
2. **Run the bulk copy** in the appropriate direction (local→remote or remote→local) for `cdnFiles`, `wwwFiles`, and `privateFiles`.
3. **Update `config.json`** — change `isLocalFileSystem` to the new value.
4. **Restart the application** so the `FileController` instances are reconstructed with the new flag.
5. **Verify** by spot-checking file reads across the site.

---

## Configuration Reference

The following `config.json` fields (in `ServerConfigModel`) control filesystem behavior:

| Field | Type | Default | Description |
|---|---|---|---|
| `isLocalFileSystem` | bool | `true` | If true, files are stored locally. If false, cdnFiles, wwwFiles, and privateFiles use AWS S3 with a local mirror. |
| `localDataDriveLetter` | string | `"D"` | Drive letter for local file storage. Subfolders follow the pattern `{drive}:\inetpub\{appName}\`. |
| `awsBucketName` | string | `""` | S3 bucket name for remote file storage. Required when `isLocalFileSystem=false`. |
| `awsRegionName` | string | `""` | AWS region (e.g., `us-east-1`). Required when `isLocalFileSystem=false`. |
| `awsAccessKey` | string | `""` | AWS access key. Can be omitted if using IAM roles. |
| `awsSecretAccessKey` | string | `""` | AWS secret key. Can be omitted if using IAM roles. |

Per-application path overrides are available in the app config:

| Field | Description |
|---|---|
| `localFilesPath` | Local path for CDN files |
| `localWwwPath` | Local path for WWW files |
| `localPrivatePath` | Local path for private files |
| `localTempPath` | Local path for temp files |
| `remoteFilePath` | S3 prefix for CDN files (default: `/{appName}/files/`) |
| `remoteWwwPath` | S3 prefix for WWW files (default: `/{appName}/www/`) |
| `remotePrivatePath` | S3 prefix for private files (default: `/{appName}/private/`) |

---

## Related Patterns

- [Contensive Architecture](contensive-architecture.md) — overall system architecture
- [Filesystem & Utilities API](api-filesystem-utilities-reference.md) — full API reference for `CPFileSystemBaseClass`
- [Redis Cache Setup](redis-cache-setup.md) — configuring remote cache for multi-server deployments
- [Secrets Management](secrets-management-pattern.md) — managing AWS credentials and config storage
- [Build Script Pattern](build-script-pattern.md) — how UI asset folders map to filesystem targets during deployment
- [Layout Design Pattern](layout-design-pattern.md) — layout file packaging and installation
- [Addon Collection Pattern](addon-collection-pattern.md) — resource file deployment to filesystem targets

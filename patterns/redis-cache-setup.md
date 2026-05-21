# Redis Remote Cache Setup (AWS ElastiCache)

Contensive supports three cache modes: local memory, local file, and remote (Redis). This document covers setting up AWS ElastiCache for Redis as the remote cache, configuring Contensive to use it, and testing the connection.

## Cache Modes

| Mode | Config property | Use case | Notes |
|------|----------------|----------|-------|
| **Remote (Redis)** | `enableRemoteCache` | Production, scale-out | Required when running multiple application instances. All instances share the same cache through Redis, ensuring consistency. |
| **Local Memory** | `enableLocalMemoryCache` | Single-server deployments only | Uses .NET `MemoryCache`. Each application instance has its own isolated cache, so this mode must not be used in scale-out (multi-instance) deployments — instances will serve stale data because cache invalidation on one instance is invisible to the others. |
| **Local File** | `enableLocalFileCache` | Debugging only | Writes cache entries to disk files under `appCache/` using mutex-based locking. This mode is slow, does not scale, and exists only for debugging cache behavior by inspecting serialized cache documents on disk. Do not use in production. |

For any deployment running behind a load balancer or with more than one application process, use Redis.

## Prerequisites

- An AWS account with permissions to create ElastiCache resources, VPC security groups, and EC2 instances
- AWS CLI installed and configured (`aws configure`)
- The Contensive application server running on an EC2 instance (or ECS/EKS) within an AWS VPC
- The Contensive CLI (`cc`) built and available on the server

## 1. Create a Security Group for Redis

Create a security group that allows inbound Redis traffic (port 6379) from your application servers.

```bash
# Get your VPC ID (use the VPC where your application runs)
aws ec2 describe-vpcs --query "Vpcs[0].VpcId" --output text

# Create security group for Redis
aws ec2 create-security-group \
  --group-name contensive-redis-sg \
  --description "Security group for Contensive Redis cache" \
  --vpc-id <your-vpc-id>

# Allow inbound on port 6379 from your application security group
aws ec2 authorize-security-group-ingress \
  --group-id <redis-sg-id> \
  --protocol tcp \
  --port 6379 \
  --source-group <app-server-sg-id>
```

Replace `<your-vpc-id>`, `<redis-sg-id>`, and `<app-server-sg-id>` with your actual values.

## 2. Create an ElastiCache Subnet Group

ElastiCache requires a subnet group that specifies which subnets the cache nodes can be placed in. Use private subnets in the same VPC as your application.

```bash
# List available subnets in your VPC
aws ec2 describe-subnets \
  --filters "Name=vpc-id,Values=<your-vpc-id>" \
  --query "Subnets[*].{SubnetId:SubnetId,AZ:AvailabilityZone,CIDR:CidrBlock}" \
  --output table

# Create the subnet group using private subnets
aws elasticache create-cache-subnet-group \
  --cache-subnet-group-name contensive-redis-subnets \
  --cache-subnet-group-description "Subnets for Contensive Redis cache" \
  --subnet-ids <subnet-1> <subnet-2>
```

## 3. Create the ElastiCache Redis Cluster

### Option A: Single-Node (Development/Testing)

A single `cache.t3.micro` node is suitable for development and testing.

```bash
aws elasticache create-cache-cluster \
  --cache-cluster-id contensive-cache \
  --engine redis \
  --cache-node-type cache.t3.micro \
  --num-cache-nodes 1 \
  --cache-subnet-group-name contensive-redis-subnets \
  --security-group-ids <redis-sg-id> \
  --engine-version 7.1
```

### Option B: Replication Group (Production)

For production, use a replication group with automatic failover for high availability.

```bash
aws elasticache create-replication-group \
  --replication-group-id contensive-cache \
  --replication-group-description "Contensive production Redis cache" \
  --engine redis \
  --cache-node-type cache.r7g.large \
  --num-cache-clusters 2 \
  --automatic-failover-enabled \
  --multi-az-enabled \
  --cache-subnet-group-name contensive-redis-subnets \
  --security-group-ids <redis-sg-id> \
  --engine-version 7.1 \
  --at-rest-encryption-enabled \
  --transit-encryption-enabled
```

### Wait for the Cluster to Become Available

```bash
# For single-node cluster
aws elasticache describe-cache-clusters \
  --cache-cluster-id contensive-cache \
  --show-cache-node-info \
  --query "CacheClusters[0].CacheNodes[0].Endpoint" \
  --output table

# For replication group
aws elasticache describe-replication-groups \
  --replication-group-id contensive-cache \
  --query "ReplicationGroups[0].NodeGroups[0].PrimaryEndpoint" \
  --output table
```

The endpoint will look like: `contensive-cache.xxxxxx.0001.use1.cache.amazonaws.com:6379`

## 4. Verify Network Connectivity

From your application server (EC2 instance), verify that the Redis endpoint is reachable.

```bash
# Install redis-cli if not present
sudo yum install -y redis6  # Amazon Linux 2
# or
sudo apt-get install -y redis-tools  # Ubuntu/Debian

# Test connectivity
redis-cli -h contensive-cache.xxxxxx.0001.use1.cache.amazonaws.com -p 6379 ping
# Expected response: PONG

# If using transit encryption (TLS), use --tls flag
redis-cli --tls -h contensive-cache.xxxxxx.0001.use1.cache.amazonaws.com -p 6379 ping
```

If the connection times out, verify:
1. The application server's security group allows outbound traffic on port 6379
2. The Redis security group allows inbound from the application server's security group
3. Both are in the same VPC or have VPC peering configured
4. The subnet route tables allow traffic between the subnets

## 5. Configure Contensive

### Option A: Using the CLI (Interactive)

Run the Contensive CLI configure command:

```bash
cc --configure
```

When prompted for cache configuration:
1. Select `r` for Redis server
2. Enter the ElastiCache endpoint in `server:port` format, e.g.:
   ```
   contensive-cache.xxxxxx.0001.use1.cache.amazonaws.com:6379
   ```
3. The CLI will test the connection and report success or failure

### Option B: Edit config.json Directly

The server configuration is stored in `config.json` on the application server. Set these three properties:

```json
{
  "enableLocalMemoryCache": false,
  "enableLocalFileCache": false,
  "enableRemoteCache": true,
  "awsElastiCacheConfigurationEndpoint": "contensive-cache.xxxxxx.0001.use1.cache.amazonaws.com:6379"
}
```

**Important**: When `enableRemoteCache` is true, set `enableLocalMemoryCache` and `enableLocalFileCache` to false. Redis is the shared source of truth across all application instances. Enabling local memory alongside remote can cause stale reads when one instance invalidates a key that another instance still holds in its local memory. Local file mode is for debugging only and should never be enabled in production.

### TLS Connections

If transit encryption is enabled on the ElastiCache cluster, append `,ssl=true` to the endpoint:

```json
{
  "awsElastiCacheConfigurationEndpoint": "contensive-cache.xxxxxx.0001.use1.cache.amazonaws.com:6379,ssl=true"
}
```

The endpoint string is passed directly to `StackExchange.Redis.ConfigurationOptions.Parse()`, so any [StackExchange.Redis configuration option](https://stackexchange.github.io/StackExchange.Redis/Configuration.html) can be appended as a comma-separated value. Common options:

| Option | Example | Description |
|--------|---------|-------------|
| `ssl` | `ssl=true` | Enable TLS |
| `password` | `password=secret` | AUTH password |
| `connectTimeout` | `connectTimeout=10000` | Connection timeout (ms), overrides the 5000ms default set in code |
| `syncTimeout` | `syncTimeout=5000` | Sync operation timeout (ms) |
| `abortConnect` | `abortConnect=false` | Already set in code; don't abort if initial connect fails |

## 6. Test the Connection

### From the Contensive CLI

```bash
cc --configure
# Select 'r' for Redis, enter endpoint, observe "success" or "fail"
```

The CLI calls `CacheController.testConnection()` which opens a connection, gets a database reference, and returns true if no exception is thrown.

### From redis-cli (Manual Verification)

```bash
# Write and read a test key
redis-cli -h <endpoint> -p 6379

> SET contensive-test "hello"
OK
> GET contensive-test
"hello"
> DEL contensive-test
(integer) 1
> QUIT
```

### From the Application (Runtime Verification)

After configuring and restarting the application, check the NLog output. On startup, the `CacheController` constructor logs:
- **Success**: No error log entries related to Redis; `remoteCacheInitialized` will be true
- **Connection failure**: `Exception initializing Redis connection, will continue with cache disabled.`
- **Ongoing issues**: `Redis connection failed: <message>` from the `ConnectionFailed` event handler
- **Recovery**: `Redis connection restored` from the `ConnectionRestored` event handler

To verify cache operations are flowing through Redis, enable NLog trace-level logging for the `CacheController` class temporarily:

```xml
<!-- In NLog.config, add this rule -->
<logger name="Contensive.Processor.Controllers.CacheController" minlevel="Trace" writeTo="yourTarget" />
```

You should see log entries like:
```
cache hit, cacheType [remote], key [...]
cacheType [remote], key [...], invalidationDate [...]
```

## 7. Monitoring

### CloudWatch Metrics

ElastiCache publishes metrics to CloudWatch automatically. Key metrics to monitor:

- **CurrConnections**: Active connections (watch for connection exhaustion)
- **CacheHitRate**: Ratio of hits to total requests (low rate may indicate keys are expiring too quickly)
- **EngineCPUUtilization**: Redis process CPU usage
- **DatabaseMemoryUsagePercentage**: Memory consumption
- **Evictions**: Keys evicted due to memory pressure (should be 0 in normal operation)

```bash
# Check current connection count
aws cloudwatch get-metric-statistics \
  --namespace AWS/ElastiCache \
  --metric-name CurrConnections \
  --dimensions Name=CacheClusterId,Value=contensive-cache \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average
```

### Redis INFO Command

```bash
redis-cli -h <endpoint> -p 6379 INFO stats | grep -E "keyspace_hits|keyspace_misses|connected_clients|used_memory_human"
```

## 8. Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `testConnection` returns false | Endpoint incorrect or unreachable | Verify endpoint string, security groups, and VPC routing |
| `RedisConnectionException` on startup | Redis not available, DNS not resolving | Check ElastiCache cluster status; verify DNS resolution from app server |
| Timeout exceptions on read/write | Network latency or Redis overloaded | Check CloudWatch CPU/memory; consider upgrading node type |
| Cache misses but Redis has the key | `globalInvalidationDate` is newer than cached data | This is expected after `invalidateAll()`; data will be re-cached on next access |
| `remote cache write failed` in logs | Transient Redis error | The application falls back to local cache automatically; check Redis health if persistent |
| `remote cache read failed, falling back to local` | Transient Redis error during read | Same as above; reads fall through to local memory/file cache |

# Secrets Management Pattern

> All patterns and API reference: [Patterns Index](index.md)

> Source files: `source/Processor/Models/Domain/SecretsModel.cs`, `source/Processor/Models/Domain/ServerConfigModel.cs`, `source/Processor/Controllers/Aws/AwsSecretManagerController.cs`

---

## Overview

Contensive supports two configuration storage modes:

| Mode | `useSecretManager` | Description |
|------|-------------------|-------------|
| **File-based** | `false` (default) | All settings stored in `config.json` on the local filesystem |
| **AWS Secrets Manager** | `true` | Full configuration stored in AWS Secrets Manager; `config.json` contains only bootstrap fields |

Both modes produce the same in-memory `ServerConfigModel` at startup. All application code reads configuration the same way regardless of which storage backend is active.

---

## How It Works

### File-Based Mode (Default)

The full `ServerConfigModel` JSON lives in `config.json` on disk (typically `D:\Contensive\config.json`). The application reads, deserializes, and uses it directly.

```
config.json (full) --> deserialize --> ServerConfigModel
```

### Secrets Manager Mode

When `useSecretManager` is `true`, `config.json` is a minimal bootstrap file containing only the fields needed to connect to AWS Secrets Manager:

```json
{
  "useSecretManager": true,
  "awsRegionName": "us-east-1",
  "awsSecretName": "contensive/myServerGroup"
}
```

The `awsSecretName` defaults to `contensive/{serverName}` based on the server group name configured during `cc --configure`. This keeps secrets isolated when multiple server groups share the same AWS account.

At startup, the application:
1. Reads `config.json` and deserializes the bootstrap fields
2. Uses `awsRegionName` and `awsSecretName` to fetch the full configuration JSON from AWS Secrets Manager
3. Deserializes the SM response into a `ServerConfigModel`
4. Preserves the bootstrap fields (`useSecretManager`, `awsRegionName`, `awsSecretName`) from the local file

```
config.json (bootstrap) --> read SM secret --> deserialize --> ServerConfigModel
```

When saving, the process reverses: the full config is written to SM, and only the bootstrap fields are written to `config.json`.

### Authentication to AWS

The `AmazonSecretsManagerClient` is created with only a region parameter, which uses the [AWS default credential provider chain](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html):

1. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
2. AWS credentials file (`~/.aws/credentials`)
3. **EC2 instance profile / IAM role** (recommended for EC2)
4. **ECS task role** (recommended for Docker on ECS)

No AWS access keys need to be stored in `config.json` when using IAM roles.

---

## Architecture

### Key Files

| File | Role |
|------|------|
| `source/CPBase/BaseModels/ServerConfigBaseModel.cs` | Abstract base defining all config properties including `useSecretManager` and `awsSecretName` |
| `source/Processor/Models/Domain/ServerConfigModel.cs` | Concrete config model with `create()` (load) and `save()` methods that handle both modes |
| `source/Processor/Models/Domain/SecretsModel.cs` | Facade for reading/writing secrets; delegates server-level secrets to `serverConfig`, routes custom app-level secrets to SM or `appConfig.secrets` |
| `source/Processor/Controllers/Aws/AwsSecretManagerController.cs` | Low-level AWS SM API calls (get/set with upsert) |
| `source/Cli/Views/ConfigureCmd.cs` | CLI wizard that prompts for SM configuration |

### Secret Categories

**Server-level secrets** are properties of `ServerConfigModel` loaded at startup from whichever source is active. Code accesses them through `core.secrets.*`:

- `core.secrets.awsAccessKey`
- `core.secrets.awsSecretAccessKey`
- `core.secrets.defaultDataSourceAddress`
- `core.secrets.defaultDataSourceUsername`
- `core.secrets.defaultDataSourcePassword`

**App-level custom secrets** are name-value pairs specific to an application. These are accessed through `core.secrets.getSecret(name)` and `core.secrets.setSecret(name, value)`. When using SM, each custom secret is stored as an individual SM secret. When using file-based mode, they are stored in the `appConfig.secrets` list within `config.json`.

---

## Setup Guide: EC2 Instance

All AWS CLI commands use PowerShell syntax. The AWS CLI works the same on Windows and Linux; only the shell quoting and line continuation differ.

### Prerequisites

- An EC2 instance running the Contensive application
- [AWS CLI for Windows](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html) installed and configured (`aws configure`)
- IAM permissions to create roles, policies, and Secrets Manager secrets

### Step 1: Create an IAM Policy for Secrets Manager Access

Create a policy that grants read/write access to Contensive secrets. Save the following as `contensive-sm-policy.json`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:CreateSecret",
        "secretsmanager:PutSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": "arn:aws:secretsmanager:*:*:secret:contensive/*"
    }
  ]
}
```

Then create the policy:

```powershell
aws iam create-policy `
  --policy-name ContensiveSecretsManagerAccess `
  --policy-document file://contensive-sm-policy.json
```

The `Resource` pattern `contensive/*` matches the default secret name prefix. Adjust if you use a different `awsSecretName`.

### Step 2: Create an IAM Role for the EC2 Instance

Save the following as `ec2-trust-policy.json`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": { "Service": "ec2.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

Then create the role, attach the policy, and create the instance profile:

```powershell
# Create the role with EC2 trust policy
aws iam create-role `
  --role-name ContensiveServerRole `
  --assume-role-policy-document file://ec2-trust-policy.json

# Attach the Secrets Manager policy (replace <account-id> with your AWS account ID)
aws iam attach-role-policy `
  --role-name ContensiveServerRole `
  --policy-arn arn:aws:iam::<account-id>:policy/ContensiveSecretsManagerAccess

# Create an instance profile and add the role
aws iam create-instance-profile `
  --instance-profile-name ContensiveServerProfile

aws iam add-role-to-instance-profile `
  --instance-profile-name ContensiveServerProfile `
  --role-name ContensiveServerRole
```

### Step 3: Attach the Instance Profile to the EC2 Instance

For an existing instance:

```powershell
aws ec2 associate-iam-instance-profile `
  --instance-id <instance-id> `
  --iam-instance-profile Name=ContensiveServerProfile
```

For new instances, specify the instance profile at launch time.

### Step 4: Create the Initial Secret in AWS Secrets Manager

Copy the current `config.json` content into a Secrets Manager secret. Replace `myServerGroup` with your server group name from `cc --configure`:

```powershell
# Create the secret from the current config file (replace myServerGroup with your server group name)
aws secretsmanager create-secret `
  --name "contensive/myServerGroup" `
  --description "Contensive server configuration" `
  --secret-string file://D:\Contensive\config.json `
  --region us-east-1
```

### Step 5: Update config.json to Bootstrap Mode

Replace the full `config.json` with the minimal bootstrap version:

```json
{
  "useSecretManager": true,
  "awsRegionName": "us-east-1",
  "awsSecretName": "contensive/myServerGroup"
}
```

### Step 6: Verify

Restart the Contensive application and verify it starts successfully. Check NLog output for any errors related to Secrets Manager access.

```powershell
# Verify the secret is readable (replace myServerGroup with your server group name)
aws secretsmanager get-secret-value `
  --secret-id "contensive/myServerGroup" `
  --region us-east-1 `
  --query "SecretString" `
  --output text | python -m json.tool
```

---

## Setup Guide: Docker (ECS)

### Prerequisites

- A Docker image with the Contensive application
- An ECS cluster (Fargate or EC2-backed)
- AWS CLI installed and configured

### Step 1: Create the IAM Policy

Same as EC2 Step 1 above.

### Step 2: Create an ECS Task Execution Role and Task Role

The **task execution role** is used by ECS to pull images and write logs. The **task role** is used by the application inside the container to access AWS services.

Save the following as `ecs-trust-policy.json`:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": { "Service": "ecs-tasks.amazonaws.com" },
      "Action": "sts:AssumeRole"
    }
  ]
}
```

Then create the role and attach the policy:

```powershell
# Create the task role (used by the application)
aws iam create-role `
  --role-name ContensiveEcsTaskRole `
  --assume-role-policy-document file://ecs-trust-policy.json

# Attach the Secrets Manager policy to the task role (replace <account-id>)
aws iam attach-role-policy `
  --role-name ContensiveEcsTaskRole `
  --policy-arn arn:aws:iam::<account-id>:policy/ContensiveSecretsManagerAccess
```

### Step 3: Create the Initial Secret

Same as EC2 Step 4 above.

### Step 4: Configure the Bootstrap config.json in the Container

There are several approaches to provide the bootstrap `config.json` to the container:

**Option A: Bake into the Docker image**

Add the bootstrap config to your Dockerfile:

```dockerfile
RUN mkdir -p /d/Contensive
COPY config.bootstrap.json /d/Contensive/config.json
```

Where `config.bootstrap.json` contains:

```json
{
  "useSecretManager": true,
  "awsRegionName": "us-east-1",
  "awsSecretName": "contensive/myServerGroup"
}
```

**Option B: Mount via ECS task definition**

Use an EFS volume or an S3-backed init container to provide the file at runtime.

**Option C: Environment variables (entrypoint script)**

Write a container entrypoint script that generates `config.json` from environment variables. Since Docker containers run Linux, this script uses bash:

```bash
#!/bin/bash
mkdir -p /d/Contensive
cat > /d/Contensive/config.json << EOF
{
  "useSecretManager": true,
  "awsRegionName": "${AWS_REGION:-us-east-1}",
  "awsSecretName": "${AWS_SECRET_NAME:-contensive/myServerGroup}"
}
EOF
exec "$@"
```

Then in your ECS task definition, set the environment variables `AWS_REGION` and `AWS_SECRET_NAME`.

### Step 5: Reference the Task Role in the Task Definition

In your ECS task definition JSON:

```json
{
  "family": "contensive",
  "taskRoleArn": "arn:aws:iam::<account-id>:role/ContensiveEcsTaskRole",
  "executionRoleArn": "arn:aws:iam::<account-id>:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "contensive",
      "image": "<your-ecr-image>",
      "essential": true
    }
  ]
}
```

The `taskRoleArn` makes AWS credentials available to the application via the ECS credential provider, which the AWS SDK picks up automatically.

### Step 6: Deploy and Verify

```powershell
# Register and run the task
aws ecs register-task-definition --cli-input-json file://task-definition.json
aws ecs update-service --cluster <cluster> --service <service> --task-definition contensive

# Check logs for startup errors
aws logs get-log-events `
  --log-group-name <log-group> `
  --log-stream-name <stream> `
  --limit 50
```

---

## Converting an Existing File-Based Server to Secrets Manager

This procedure converts a running Contensive server from file-based configuration to AWS Secrets Manager with zero downtime.

### Step 1: Ensure IAM Access

Set up IAM roles/policies as described in the EC2 or Docker setup guides above. Verify the application server can reach AWS Secrets Manager:

```powershell
aws secretsmanager list-secrets --region us-east-1
```

If this fails, check IAM roles, VPC endpoints (for private subnets), and security groups.

### Step 2: Back Up the Current config.json

```powershell
Copy-Item D:\Contensive\config.json D:\Contensive\config.json.backup
```

### Step 3: Push the Current Config to Secrets Manager

Use your server group name (from the `name` field in `config.json`) as the secret name:

```powershell
# Replace myServerGroup with your server group name
aws secretsmanager create-secret `
  --name "contensive/myServerGroup" `
  --description "Contensive server configuration" `
  --secret-string file://D:\Contensive\config.json `
  --region us-east-1
```

### Step 4: Verify the Secret Content

```powershell
aws secretsmanager get-secret-value `
  --secret-id "contensive/myServerGroup" `
  --region us-east-1 `
  --query "SecretString" `
  --output text | python -m json.tool
```

Compare the output with the original `config.json` to confirm they match.

### Step 5: Switch to Bootstrap Mode

Replace `config.json` with the minimal bootstrap file. Note the `awsRegionName` must match the region where the secret was created:

```json
{
  "useSecretManager": true,
  "awsRegionName": "us-east-1",
  "awsSecretName": "contensive/myServerGroup"
}
```

### Step 6: Restart and Verify

Restart the Contensive application. It will:
1. Read the bootstrap `config.json`
2. Fetch the full configuration from `contensive/myServerGroup` in Secrets Manager
3. Start normally with all settings intact

Check NLog output for errors. If something goes wrong, restore the backup:

```powershell
Copy-Item D:\Contensive\config.json.backup D:\Contensive\config.json
```

And restart to revert to file-based mode.

### Step 7: Remove AWS Keys from the Secret (Optional)

If you are using IAM roles for authentication, the `awsAccessKey` and `awsSecretAccessKey` fields in the SM secret are no longer needed for SM access (they may still be needed for other AWS services like S3). You can clear them from the stored config if the IAM role covers all required services:

```powershell
# Fetch current secret, remove keys, update (replace myServerGroup with your server group name)
$secret = aws secretsmanager get-secret-value `
  --secret-id "contensive/myServerGroup" `
  --query "SecretString" `
  --output text | ConvertFrom-Json

$secret.awsAccessKey = ""
$secret.awsSecretAccessKey = ""
$updatedJson = $secret | ConvertTo-Json -Depth 10 -Compress

aws secretsmanager put-secret-value `
  --secret-id "contensive/myServerGroup" `
  --secret-string $updatedJson
```

### Using the CLI Configure Command (Recommended)

The simplest way to convert is through the interactive CLI, which detects existing file-based configuration and offers to migrate automatically:

```powershell
cc --configure
```

When prompted:
1. Enter AWS credentials (or leave blank if using IAM roles)
2. Select **y** for "Use AWS Secrets Manager"
3. Enter the secret name (default: `contensive/{serverName}` based on your server group name)
4. Enter the AWS region
5. **Automatic migration prompt**: If the Secrets Manager secret is empty and the local `config.json` contains configuration data, the CLI will ask: *"Do you want to migrate your config file to secret manager (y/n)?"* — answering **y** copies the current config.json content to Secrets Manager immediately
6. Complete the remaining configuration prompts

On save, the CLI writes the full configuration to Secrets Manager and reduces the local `config.json` to the minimal bootstrap file automatically.

---

## Rollback to File-Based Mode

To revert from Secrets Manager to file-based mode:

1. Fetch the current config from SM (replace `myServerGroup` with your server group name):
   ```powershell
   aws secretsmanager get-secret-value `
     --secret-id "contensive/myServerGroup" `
     --region us-east-1 `
     --query "SecretString" `
     --output text | Out-File -Encoding utf8 D:\Contensive\config.json
   ```

2. Set `useSecretManager` to `false` in the file:
   ```powershell
   $config = Get-Content D:\Contensive\config.json | ConvertFrom-Json
   $config.useSecretManager = $false
   $config | ConvertTo-Json -Depth 10 | Set-Content D:\Contensive\config.json
   ```

3. Restart the application. It will read the full config from the local file.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `InvalidOperationException: useSecretManager is true but awsRegionName is not configured` | Bootstrap `config.json` missing `awsRegionName` | Add `awsRegionName` to the bootstrap config.json |
| `InvalidOperationException: AWS Secrets Manager secret [...] is empty or not found` | Secret doesn't exist or is empty | Create the secret with `aws secretsmanager create-secret` |
| `AmazonSecretsManagerException: Access denied` | IAM role or policy missing | Verify the instance profile (EC2) or task role (ECS) has the `ContensiveSecretsManagerAccess` policy |
| `AmazonSecretsManagerException: Unable to connect` | No network path to SM endpoint | For private subnets, create a VPC endpoint for Secrets Manager |
| Application starts but config values are wrong | Stale secret in SM | Fetch and inspect the secret: `aws secretsmanager get-secret-value --secret-id contensive/{serverName}` |
| `ResourceNotFoundException` on save | Secret was deleted from SM | The save method will automatically create a new secret |

---

## Security Considerations

- **IAM roles over access keys**: Always prefer EC2 instance profiles or ECS task roles. Avoid storing AWS access keys in `config.json` or the SM secret itself.
- **Least privilege**: The IAM policy should scope the `Resource` to only the secrets your server needs (e.g., `arn:aws:secretsmanager:*:*:secret:contensive/*`).
- **Encryption**: AWS Secrets Manager encrypts secrets at rest using AWS KMS. The default `aws/secretsmanager` key is used unless you specify a customer-managed KMS key.
- **Rotation**: AWS SM supports automatic secret rotation. This is not currently integrated with Contensive but could be added for database credentials.
- **Audit**: Enable AWS CloudTrail to log all Secrets Manager API calls for auditing who accessed or modified the configuration.
- **VPC endpoints**: For production deployments in private subnets, create a VPC endpoint for Secrets Manager to avoid routing traffic over the public internet.

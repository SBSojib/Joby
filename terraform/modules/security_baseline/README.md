# Security baseline module

Terraform module that provisions **account-level security and audit** services for Joby: a private S3 bucket for CloudTrail and AWS Config delivery, a multi-Region CloudTrail trail, a GuardDuty detector, Security Hub enrollment, and an AWS Config recorder with delivery channel. It does not attach to the application VPC, EKS, or RDS security groups.

Root wiring lives in [`terraform/main.tf`](../../main.tf) (`module "security_baseline"`). Only `project_name`, `environment`, and tags are passed from root; no other Joby modules supply inputs to this module.

## Resources in this module

The module defines **nineteen Terraform constructs** (fifteen create AWS or random state; four are read-only data sources or IAM policy documents). Together they centralize audit logs in S3 and turn on core detective and configuration services in the deployment Region.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `locals` (`name_prefix`) | Terraform locals | Prefix for bucket, trail, Config, and IAM **names** (`${project_name}-${environment}`) | Used by S3, CloudTrail, Config recorder, delivery channel, and `aws_iam_role.config` |
| `data.aws_caller_identity.current` | AWS account identity (data) | Resolves **current account ID** for S3 key paths in bucket policy and Config delivery prefix | Used in `data.aws_iam_policy_document.audit_logs` and `aws_config_delivery_channel.this.s3_key_prefix` |
| `data.aws_region.current` | Current Region (data) | Standard Region lookup | Declared in [`main.tf`](main.tf); **not referenced** by other objects in this module |
| `random_id.suffix` | Terraform random suffix | Makes the audit **S3 bucket name** globally unique | Hex suffix in `aws_s3_bucket.audit_logs.bucket` |
| `aws_s3_bucket.audit_logs` | S3 bucket | **Durable store** for CloudTrail and AWS Config snapshots | Target for bucket policy, encryption, versioning, and public-access block; `s3_bucket_name` for CloudTrail and Config delivery |
| `aws_s3_bucket_public_access_block.audit_logs` | S3 public access block | **Blocks public** ACLs, policies, and anonymous access on the audit bucket | `bucket` = `aws_s3_bucket.audit_logs.id` |
| `aws_s3_bucket_versioning.audit_logs` | S3 versioning | **Versioning enabled** on audit objects | Same bucket as above |
| `aws_s3_bucket_server_side_encryption_configuration.audit_logs` | S3 default encryption | **SSE-S3 (AES256)** at rest for new objects | Same bucket as above |
| `data.aws_iam_policy_document.audit_logs` | S3 bucket policy (data) | Grants **CloudTrail** and **AWS Config** service principals `GetBucketAcl` and `PutObject` with bucket-owner ACL conditions | Feeds `aws_s3_bucket_policy.audit_logs`; paths include `data.aws_caller_identity.current.account_id` |
| `aws_s3_bucket_policy.audit_logs` | S3 bucket policy | Enforces the audit policy JSON on the bucket | Required before `aws_cloudtrail.this` (`depends_on`) |
| `aws_cloudtrail.this` | CloudTrail trail | **API and management event** logging to the audit bucket | `s3_bucket_name` = audit bucket; multi-Region trail with log file validation |
| `aws_guardduty_detector.this` | GuardDuty detector | **Threat detection** for the Region | Enabled in this module; not wired to other Joby modules |
| `aws_securityhub_account.this` | Security Hub account | **Enables Security Hub** on the account | No standards subscriptions or product integrations defined here |
| `data.aws_iam_policy_document.config_assume_role` | IAM trust policy (data) | Allows **`config.amazonaws.com`** to assume the Config role | Feeds `aws_iam_role.config` |
| `aws_iam_role.config` | IAM role | Identity for **AWS Config** to record and deliver configuration | `role_arn` on `aws_config_configuration_recorder.this` |
| `aws_iam_role_policy_attachment.config` | Policy attachment | Attaches AWS-managed **`AWS_ConfigRole`** | Grants Config service permissions for recording |
| `aws_config_configuration_recorder.this` | AWS Config recorder | **Records configuration** for supported resource types | `recording_group` includes all supported types and global resource types |
| `aws_config_delivery_channel.this` | AWS Config delivery channel | Delivers Config snapshots to the **audit S3 bucket** under `AWSLogs/<account_id>/Config` | `depends_on` recorder; shares bucket with CloudTrail |
| `aws_config_configuration_recorder_status.this` | Config recorder status | **Starts** the recorder after the delivery channel exists | `depends_on` `aws_config_delivery_channel.this` |

## How this module connects to the rest of the stack

**Inputs from root (`module "security_baseline"` in [`main.tf`](../../main.tf)):**

- `project_name` and `environment` from root variables (naming only).
- `tags` from `local.common_tags`.

**Not defined inside this module:**

- No dependency on [`module.network`](../network/main.tf), [`module.security`](../security/main.tf), [`module.rds`](../rds/main.tf), or [`module.s3`](../s3/main.tf). Application uploads use a **separate** S3 bucket from [`module.s3`](../s3/main.tf); this audit bucket is only for CloudTrail and Config delivery.
- **KMS keys**, **S3 lifecycle expiration**, **CloudTrail organization trails**, **Config rules**, **Security Hub standards**, and **GuardDuty member/administrator** setup are not created here.
- Root [`terraform/README.md`](../../README.md) mentions `enable_security_baseline`; that toggle is **not** present in current root [`variables.tf`](../../variables.tf)—the module is always invoked when the root stack is applied.

**Downstream consumers:**

- Root outputs `audit_log_bucket_name`, `cloudtrail_arn`, and `guardduty_detector_id` re-export module outputs for operators and compliance reference.
- No other Terraform modules in the Joby root stack read these outputs as inputs.

**Typical apply order:** `random_id` and audit S3 bucket (with public access block, versioning, encryption) → bucket policy → CloudTrail → Config IAM role and attachment → Config recorder → delivery channel → recorder status enabled. GuardDuty and Security Hub can proceed in parallel with S3/Config once the account allows those APIs. Re-applying in a Region that already has a default GuardDuty detector or an enabled Config recorder may require import or manual alignment outside this document.

## Notable parameters

### Audit storage (S3)

- **`force_destroy = false`** — Hard-coded on `aws_s3_bucket.audit_logs`. Destroying the stack does **not** empty the bucket automatically; protects audit history but can block destroy until the bucket is emptied.
- **Versioning and SSE-S3 AES256** — Hard-coded; no transition to Glacier or lifecycle expiration in this module.
- **Public access block** — All four block flags **true** (hard-coded).
- **Bucket name** — `${name_prefix}-audit-logs-${random_id.suffix.hex}`; not overridable via module variables.

### CloudTrail

- **`is_multi_region_trail = true`**, **`include_global_service_events = true`**, **`enable_log_file_validation = true`** — Hard-coded.
- **Single `event_selector`** — Management events, read/write type **All**; no data-event selectors for individual S3 objects or Lambda payloads.

### GuardDuty and Security Hub

- **`aws_guardduty_detector.this.enable = true`** — Hard-coded; per-Region detector only.
- **`aws_securityhub_account.this`** — Enables the service with **no** `aws_securityhub_standards_subscription` or custom insights in this module.

### AWS Config

- **`recording_group`** — `all_supported = true` and `include_global_resource_types = true` (hard-coded); broad inventory and **ongoing Config charges** in busy accounts.
- **Delivery** — Same audit bucket as CloudTrail; prefix `AWSLogs/<account_id>/Config` via `data.aws_caller_identity.current`.
- **`AWS_ConfigRole`** — AWS-managed policy ARN hard-coded on `aws_iam_role_policy_attachment.config`.

### Variables vs fixed HCL

- **Exposed:** `project_name`, `environment`, `tags` only.
- **Fixed in HCL:** Service enablement, trail scope, encryption mode, Config recording scope, IAM policy choice, and S3 hardening flags. No module-level switches to disable CloudTrail, GuardDuty, Config, or Security Hub.

## Module outputs

| Output | Use |
| --- | --- |
| `audit_log_bucket_name` | Root output; locate CloudTrail and Config objects in S3 |
| `cloudtrail_arn` | Root output; reference the trail in support and audit workflows |
| `guardduty_detector_id` | Root output; identify the Regional GuardDuty detector |

## Source files

- [`main.tf`](main.tf) — Locals, data sources, audit bucket, CloudTrail, GuardDuty, Security Hub, AWS Config
- [`variables.tf`](variables.tf) — Module inputs and defaults
- [`outputs.tf`](outputs.tf) — Audit bucket, CloudTrail, and GuardDuty identifiers

# S3 module

Terraform module that provisions a **private, encrypted S3 bucket** for application file uploads in the Joby stack. It applies versioning, default encryption, public-access blocks, an HTTPS-only bucket policy, object-ownership controls, and lifecycle rules on top of the base bucket.

Root wiring lives in [`terraform/main.tf`](../../main.tf) (`module "s3"`). The bucket ARN is passed to [`module.backend_irsa`](../../main.tf) so the backend Kubernetes workload can access objects through IAM Roles for Service Accounts (IRSA).

## Resources in this module

The module defines **eight Terraform objects** (seven create or update AWS resources; one is a read-only IAM policy document). Together they create a single uploads bucket and harden it for private, TLS-only access.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `aws_s3_bucket.uploads` | S3 bucket | Regional object store for application uploads | Named from `var.bucket_name` (root passes `local.s3_bucket_name`); tagged with `var.tags` (root passes `local.common_tags`). Parent for all other bucket sub-resources in this module. |
| `aws_s3_bucket_versioning.uploads` | Bucket versioning setting | Keeps **object versions** on overwrite | Targets `aws_s3_bucket.uploads.id` with `status = "Enabled"`. Works with lifecycle `noncurrent_version_expiration` below. |
| `aws_s3_bucket_server_side_encryption_configuration.uploads` | Default encryption setting | **Encrypts objects at rest** on write | Targets `aws_s3_bucket.uploads.id`; default rule uses SSE-S3 (`AES256`) with `bucket_key_enabled = true`. |
| `aws_s3_bucket_public_access_block.uploads` | Public access block | Blocks **public ACLs and bucket policies** | Targets `aws_s3_bucket.uploads.id`; all four block flags are `true`. Applied before the bucket policy (`depends_on` on `aws_s3_bucket_policy.uploads`). |
| `data.aws_iam_policy_document.uploads` | IAM policy document (data) | JSON for a **Deny unless HTTPS** statement | `resources` are the bucket ARN and `arn/*` from `aws_s3_bucket.uploads`. Feeds `policy` on `aws_s3_bucket_policy.uploads`. |
| `aws_s3_bucket_policy.uploads` | Bucket policy | Enforces **TLS-only** access (`aws:SecureTransport`) | Attaches `data.aws_iam_policy_document.uploads.json` to `aws_s3_bucket.uploads`; `depends_on` public access block. Complements (does not replace) IAM on the workload role. |
| `aws_s3_bucket_ownership_controls.uploads` | Object ownership setting | **Bucket owner owns all objects**; ACLs disabled | Targets `aws_s3_bucket.uploads.id` with `object_ownership = "BucketOwnerEnforced"`. |
| `aws_s3_bucket_lifecycle_configuration.uploads` | Lifecycle rules | **Cost and cleanup** for versions and multipart uploads | Targets `aws_s3_bucket.uploads.id`; whole-bucket `filter` (`prefix = ""`); expires noncurrent versions after 30 days; aborts incomplete multipart uploads after 7 days. |

## How this module connects to the rest of the stack

**Inputs from the root module (`module "s3"`):**

- `bucket_name` ← `local.s3_bucket_name`, which uses `var.s3_bucket_name` when set, otherwise `${var.project_name}-${var.environment}-uploads-${random_id.suffix.hex}` (see root `locals` and [`random_id.suffix`](../../main.tf)).
- `tags` ← `local.common_tags` (`Project`, `Environment`, `ManagedBy`).

**Not defined inside this module:**

- Root [`random_id.suffix`](../../main.tf) supplies the hex suffix for the **auto-generated** global bucket name when `var.s3_bucket_name` is empty (default in [`variables.tf`](../../variables.tf); example in [`s3.auto.tfvars.example`](../../s3.auto.tfvars.example)).
- [`module.backend_irsa`](../../main.tf) (source [`../irsa`](../irsa/main.tf)) grants the backend service account `s3:ListBucket` on the bucket and `s3:GetObject` / `PutObject` / `DeleteObject` on `bucket_arn/*`. It needs `module.eks` OIDC outputs plus `module.s3.bucket_arn`; it does not create the bucket or change bucket policy.
- This module is **not** the CloudTrail / AWS Config audit bucket in [`module.security_baseline`](../security_baseline/main.tf); that is a separate S3 bucket and policy set.

S3 is a **regional** service and is **not** placed in the VPC. This module does not take subnets, security groups, or routes from [`module.network`](../network/main.tf).

**Downstream consumers:**

- `module.backend_irsa` uses `module.s3.bucket_arn` for IAM `resources` on the workload role.
- Root outputs `s3_bucket_name` and `s3_bucket_arn` re-export `module.s3` for operators and CI. The `next_steps` output reminds you to set the bucket name in Kubernetes manifests and annotate the backend service account with `module.backend_irsa.role_arn`.

**Typical apply order:** `random_id.suffix` (when used for naming) → `module.s3` (bucket and sub-resources; public access block before bucket policy) → `module.eks` (OIDC for IRSA) → `module.backend_irsa` (role policy scoped to `module.s3.bucket_arn`). The bucket can be created in parallel with network and EKS; IRSA wiring runs after both the bucket and cluster OIDC exist.

## Notable parameters

### Naming and tags

- **`bucket_name` (variable)** — Globally unique S3 bucket name; only naming knob exposed by the module. Root derives it from `var.s3_bucket_name` or the `project-environment-uploads-<suffix>` pattern.
- **`tags` (variable)** — Resource tags; default `{}` in the module; root passes `local.common_tags`.

### Security and access

- **Versioning** — Hard-coded `Enabled` on `aws_s3_bucket_versioning.uploads` (not a variable). Supports recovery after overwrites; pairs with lifecycle on noncurrent versions.
- **Encryption** — Hard-coded SSE-S3 (`AES256`) with `bucket_key_enabled = true` (not KMS or a variable).
- **Public access** — All four `aws_s3_bucket_public_access_block` settings are hard-coded `true`.
- **Bucket policy** — Single `Deny` on `s3:*` when `aws:SecureTransport` is false; no `Allow` statements in this module (workload access is via IAM on `module.backend_irsa`).
- **Object ownership** — Hard-coded `BucketOwnerEnforced` (no object ACL workflow).

### Lifecycle and cost

- **Noncurrent versions** — Hard-coded expiration after **30** days (`noncurrent_version_expiration.noncurrent_days`).
- **Multipart uploads** — Hard-coded abort after **7** days (`abort_incomplete_multipart_upload.days_after_initiation`).
- **Current object versions** — No expiration or transition rules; live objects are not auto-deleted by this module.

### Not configured in this module

- No S3 access logging, replication, CORS, event notifications, or `force_destroy` on the bucket resource. No VPC endpoints or bucket policy grants to other AWS principals beyond the TLS deny.

## Module outputs

| Output | Use |
| --- | --- |
| `bucket_name` | Bucket name (same as `aws_s3_bucket.uploads.id`); root output `s3_bucket_name`, `next_steps`, and Kubernetes manifest configuration |
| `bucket_arn` | Bucket ARN; passed to `module.backend_irsa` as `s3_bucket_arn`; root output `s3_bucket_arn` |
| `bucket_regional_domain_name` | Regional hostname for the bucket; available from the module but not referenced in root `outputs.tf` today |

## Source files

- [`main.tf`](main.tf) — Bucket and sub-resources above
- [`variables.tf`](variables.tf) — `bucket_name` and `tags`
- [`outputs.tf`](outputs.tf) — Bucket name, ARN, and regional domain name

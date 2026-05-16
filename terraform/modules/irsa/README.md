# IRSA module

Terraform module that provisions **IAM Roles for Service Accounts (IRSA)** for a single Kubernetes workload: an IAM role trusted via the EKS cluster OIDC provider and an inline policy for **S3** object access on one uploads bucket. In the Joby root stack it is instantiated as `module "backend_irsa"` so the API pods can call S3 without long-lived node credentials.

Root wiring lives in [`terraform/main.tf`](../../main.tf) (`module "backend_irsa"`). Trust comes from [`module.eks`](../eks/main.tf) OIDC outputs; S3 scope comes from [`module.s3`](../s3/main.tf). The Kubernetes `ServiceAccount` annotation and `serviceAccountName` on the Deployment are applied outside Terraform.

## Resources in this module

The module defines **six Terraform constructs** (two IAM policy documents, one IAM role, one inline role policy, and locals). There are no standalone AWS data sources beyond the IAM policy document data blocks.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `locals` (`oidc_provider`, `role_name`) | Terraform locals | Strip `https://` from the OIDC issuer URL for condition keys; build IAM **role and policy names** | `oidc_provider` feeds `data.aws_iam_policy_document.assume_role` conditions; `role_name` names `aws_iam_role.this` and `aws_iam_role_policy.s3_access` |
| `data.aws_iam_policy_document.assume_role` | IAM trust policy (data) | Allows **`sts:AssumeRoleWithWebIdentity`** only for the bound Kubernetes service account subject | Federated principal `var.oidc_provider_arn` (`module.eks.oidc_provider_arn`); `aud` = `sts.amazonaws.com`; `sub` = `system:serviceaccount:${namespace}:${service_account_name}` |
| `aws_iam_role.this` | IAM role | **Runtime AWS identity** assumed by pods using the annotated service account | `assume_role_policy` from `data.aws_iam_policy_document.assume_role`; ARN exported as `role_arn` to root `backend_irsa_role_arn` |
| `data.aws_iam_policy_document.s3_access` | IAM permissions policy (data) | **S3 ListBucket** on the bucket and **Get/Put/DeleteObject** on object keys | `resources` = `var.s3_bucket_arn` and `${var.s3_bucket_arn}/*` (`module.s3.bucket_arn` at root) |
| `aws_iam_role_policy.s3_access` | Inline IAM role policy | Attaches S3 permissions to the role | `role` = `aws_iam_role.this.id`; policy JSON from `data.aws_iam_policy_document.s3_access` |

## How this module connects to the rest of the stack

**Inputs from other modules and root (`module "backend_irsa"` in [`main.tf`](../../main.tf)):**

- `oidc_provider_arn` and `oidc_provider_url` from `module.eks.oidc_provider_arn` and `module.eks.cluster_oidc_issuer_url` (requires EKS cluster and [`aws_iam_openid_connect_provider`](../eks/main.tf) in the EKS module).
- `s3_bucket_arn` from `module.s3.bucket_arn`.
- `namespace` and `service_account_name` from root `k8s_namespace` and `backend_service_account_name` (defaults `joby` and `joby-backend` in [`variables.tf`](../../variables.tf); see [`general.auto.tfvars.example`](../../general.auto.tfvars.example)).
- `project_name`, `environment`, and `tags` from root variables and `local.common_tags`.

**Not defined inside this module:**

- [`module.eks`](../eks/main.tf) creates the cluster, registers the OIDC provider, and exposes issuer URL and provider ARN.
- [`module.s3`](../s3/main.tf) creates the uploads bucket; workload access is granted here via **IAM on the role**, not via bucket policy `Allow` statements in the S3 module.
- Kubernetes [`k8s/eks/serviceaccount.yaml`](../../../k8s/eks/serviceaccount.yaml) must set `eks.amazonaws.com/role-arn` to Terraform output `backend_irsa_role_arn`; the backend Deployment must use that service account name and namespace.
- Add-on IRSA roles (External Secrets, load balancer controller, and others) live in [`module.eks_addons`](../eks_addons/main.tf), not in this reusable module.

**Downstream consumers:**

- Root output `backend_irsa_role_arn` and `next_steps` guidance for manifest annotation.
- Backend pods that mount the service account receive a projected token usable for `AssumeRoleWithWebIdentity` against this role.

**Typical apply order:** `module.eks` (cluster + OIDC provider) and `module.s3` (bucket) → `module.backend_irsa` (role and S3 policy). Kubernetes service account and workload rollout follow after the role ARN is known.

## Notable parameters

### Trust and identity (security)

- **`AssumeRoleWithWebIdentity`** — Hard-coded in the trust policy; standard IRSA flow (not `sts:AssumeRole` for IAM users).
- **OIDC conditions** — `aud` must be `sts.amazonaws.com`; `sub` must match exactly `system:serviceaccount:${var.namespace}:${var.service_account_name}`. A mismatch between Terraform variables and the live `ServiceAccount` prevents assumption.
- **`oidc_provider` local** — Issuer URL without `https://` for condition variable names; must stay aligned with `var.oidc_provider_url` from EKS.

### S3 permissions

- **ListBucket** on the bucket ARN only; object APIs on `bucket_arn/*` only. No other services, secrets, or RDS permissions on this role.
- **No `s3:*` wildcard** — Explicit actions in `data.aws_iam_policy_document.s3_access` (hard-coded action list).

### Naming and lifecycle

- **`local.role_name`** — `${project_name}-${environment}-${service_account_name}`; exposed as the IAM role **name** (not a path prefix only).
- **Inline policy** — `aws_iam_role_policy.s3_access` rather than a standalone customer-managed policy ARN.

### Exposed vs fixed in HCL

- **Variables:** `project_name`, `environment`, OIDC ARN/URL, namespace, service account name, `s3_bucket_arn`, `tags`.
- **Fixed in HCL:** Trust action, OIDC audience, S3 action set, and trust `StringEquals` tests.

## Module outputs

| Output | Use |
| --- | --- |
| `role_arn` | Root `backend_irsa_role_arn`; `eks.amazonaws.com/role-arn` on the backend `ServiceAccount` |
| `service_account_subject` | Expected OIDC `sub` claim (`system:serviceaccount:…`); useful for verifying trust policy alignment (not re-exported at root) |

## Source files

- [`main.tf`](main.tf) — Locals, trust and S3 policy documents, IAM role and inline policy
- [`variables.tf`](variables.tf) — Module inputs
- [`outputs.tf`](outputs.tf) — Role ARN and service account subject

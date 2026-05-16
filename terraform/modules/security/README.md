# Security module

Terraform module that provisions a **VPC security group** for the Joby **Amazon RDS** PostgreSQL instance. The group is attached to the database ENI in [`module.rds`](../rds/main.tf); **inbound** access is not defined here. Root [`main.tf`](../../main.tf) adds a separate ingress rule from EKS worker nodes after the cluster security group exists.

This module is **not** [`module.security_baseline`](../security_baseline/main.tf) (CloudTrail, GuardDuty, and audit S3). It only supplies the RDS firewall object and its default outbound behavior.

## Resources in this module

The module defines **one Terraform-managed AWS resource** plus an embedded lifecycle block. There are no data sources or locals.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `aws_security_group.rds` | VPC security group | **Stateful firewall** on the RDS network interface | `vpc_id` from `module.network` (root `module "security"`); `rds_security_group_id` output passed to `module.rds` as `security_group_id`; root [`aws_vpc_security_group_ingress_rule.rds_from_eks_nodes`](../../main.tf) targets this group for PostgreSQL from `module.eks.node_security_group_id` |
| `lifecycle` (`create_before_destroy` on `aws_security_group.rds`) | Terraform lifecycle | **Replace** the security group without blocking dependents that still reference the old ID | Applies only to `aws_security_group.rds` |

## How this module connects to the rest of the stack

**Inputs from other modules and root (`module "security"` in [`main.tf`](../../main.tf)):**

- `vpc_id` from `module.network.vpc_id` (security groups are VPC-scoped).
- `project_name`, `environment`, and `tags` from root `var.project_name`, `var.environment`, and `local.common_tags`.

**Not defined inside this module:**

- Root [`aws_vpc_security_group_ingress_rule.rds_from_eks_nodes`](../../main.tf) allows **TCP 5432** from `module.eks.node_security_group_id` into `module.security.rds_security_group_id`. That rule is at root so Terraform can reference the EKS node security group, which is created in [`module.eks`](../eks/main.tf), not in this module.
- [`module.rds`](../rds/main.tf) places the instance in **private subnets** (`module.network.private_subnet_ids`) and attaches this security group via `vpc_security_group_ids`; subnet placement and database ingress are separate concerns.
- [`module.security_baseline`](../security_baseline/main.tf) covers account-level audit and threat detection; it does not use this RDS security group.

**Downstream consumers:**

- `module.rds` attaches `module.security.rds_security_group_id` to the DB instance.
- The root ingress rule authorizes **EKS worker nodes** (not the Kubernetes API or load balancers by default) to reach PostgreSQL on the RDS ENI.

**Typical apply order:** `module.network` (VPC) → `module.security` (RDS security group) → `module.eks` (node security group) → `aws_vpc_security_group_ingress_rule.rds_from_eks_nodes` → `module.rds` (instance with this group attached). Until the root ingress rule exists, the RDS group has **no inbound rules** and clients cannot connect even if the instance is up.

## Notable parameters

### Network and access

- **No ingress rules in this module** — Default **deny inbound** on the RDS ENI until root adds the EKS-node rule. There is no CIDR-based database access, bastion rule, or on-prem VPN rule in this stack’s Terraform.
- **`egress` to `0.0.0.0/0`, all protocols** — Hard-coded in [`main.tf`](main.tf). Allows outbound from the RDS ENI (for example DNS, patch metadata, and AWS service endpoints) without per-destination rules in this module.
- **`description` on the security group** — States intent (PostgreSQL from approved application security groups); the **enforcement** is the root ingress rule referencing the node security group.

### Naming and lifecycle

- **`name_prefix`** — `${project_name}-${environment}-rds-` (variable-driven prefix; AWS appends a unique suffix). **`Name` tag** — `${project_name}-${environment}-rds` via `merge(var.tags, …)`.
- **`create_before_destroy = true`** — Hard-coded lifecycle on the security group to reduce replace-time churn when the group must be recreated.

### Fixed in HCL (not module variables)

- Egress shape, security group description text, lifecycle policy, and the choice to expose only an RDS security group (no EKS, ALB, or bastion groups here).

### Distinction from other “security” modules

- **`module.security_baseline`** — Separate root module for CloudTrail, Config-related storage, and GuardDuty; not wired to `rds_security_group_id`.

## Module outputs

| Output | Use |
| --- | --- |
| `rds_security_group_id` | Attached to `aws_db_instance` in `module.rds`; target for root `aws_vpc_security_group_ingress_rule.rds_from_eks_nodes` |

## Source files

- [`main.tf`](main.tf) — RDS VPC security group and lifecycle
- [`variables.tf`](variables.tf) — Module inputs and defaults
- [`outputs.tf`](outputs.tf) — RDS security group ID for RDS and root ingress

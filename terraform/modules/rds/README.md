# RDS module

Terraform module that provisions a **private PostgreSQL** instance on **Amazon RDS** in the Joby VPC: subnet placement, enhanced monitoring IAM, backups, and connection outputs for the rest of the stack.

Root wiring lives in [`terraform/main.tf`](../../main.tf) (`module "rds"`). Connection details flow to [`module.secrets`](../../main.tf) via `module.rds.address` and `module.rds.port`.

## Resources in this module

The module defines **six Terraform objects** (five create AWS or random state; one is a read-only IAM policy document). Together they place a managed database in private subnets, attach monitoring permissions, and expose endpoints for applications.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `aws_db_subnet_group.this` | RDS subnet group | Tells RDS **which VPC subnets** may host database network interfaces | Built from `subnet_ids` (root passes **private subnets** from `module.network`). Referenced by `aws_db_instance` via `db_subnet_group_name`. Multi-AZ needs subnets in **at least two AZs**. |
| `random_id.final_snapshot` | Terraform random suffix | Makes **final snapshot names unique** on destroy | Hex suffix used in `final_snapshot_identifier` when snapshots are not skipped. |
| `data.aws_iam_policy_document.enhanced_monitoring_assume_role` | IAM trust policy (data) | JSON allowing **`monitoring.rds.amazonaws.com`** to assume a role | Feeds `assume_role_policy` on `aws_iam_role.enhanced_monitoring`. |
| `aws_iam_role.enhanced_monitoring` | IAM role | Identity for **RDS enhanced monitoring** (OS-level metrics to CloudWatch) | ARN passed to the DB as `monitoring_role_arn`. |
| `aws_iam_role_policy_attachment.enhanced_monitoring` | Policy attachment | Grants AWS-managed **AmazonRDSEnhancedMonitoringRole** | Role must exist before the instance uses it; `depends_on` enforces that order. |
| `aws_db_instance.this` | RDS DB instance | The **managed PostgreSQL** server: compute, storage, backups, HA, logs, Performance Insights | Uses subnet group + `security_group_id`; exposes **endpoint / address / port** to root (e.g. `module.secrets`). |

## How this module connects to the rest of the stack

**Inputs from other modules (root `module "rds"`):**

- `subnet_ids` ← `module.network.private_subnet_ids` (database ENIs in private subnets).
- `security_group_id` ← `module.security.rds_security_group_id` (RDS firewall attachment).

**Not defined inside this module:**

- [`module.security`](../security/main.tf) creates the RDS **security group** (egress only in that module).
- Root [`aws_vpc_security_group_ingress_rule.rds_from_eks_nodes`](../../main.tf) allows **PostgreSQL (TCP 5432)** from the EKS **node** security group into the RDS security group.

This module only **attaches** the security group ID; it does not define who may connect.

**Downstream consumers:**

- `module.secrets` uses `module.rds.address`, `module.rds.port`, and database credentials for application connection settings.

**Typical apply order:** DB subnet group and monitoring IAM (with policy attached) → RDS instance → module outputs consumed by secrets and the backend.

## Notable parameters on `aws_db_instance`

### Engine and sizing

- **`engine` / `engine_version`** — PostgreSQL **16**; AWS selects compatible minor/patch levels unless pinned elsewhere.
- **`instance_class`** — CPU/RAM/network SKU (default `db.t3.medium` via variable). Main **performance and cost** knob.

### Storage

- **`allocated_storage` / `max_allocated_storage`** — Starting disk size and **autoscale ceiling** on gp3.
- **`storage_type = "gp3"`** — General-purpose SSD; typical default for OLTP-style workloads.
- **`storage_encrypted = true`** — Encryption at rest.

### Network and access

- **`db_subnet_group_name`** — Placement in private subnets.
- **`vpc_security_group_ids`** — Stateful firewall on the DB ENI; must align with root ingress from EKS nodes.
- **`publicly_accessible = false`** — No internet-routable endpoint; reachability is VPC/private paths only.
- **`multi_az`** — Synchronous standby in another AZ and **managed failover** (one primary endpoint; AWS-driven failover).

### Credentials and initial database

- **`db_name`, `username`, `password`** — Initial database and master login at **first provision** (rotation is a separate operational concern).

### Backup, maintenance, and lifecycle

- **`backup_retention_period`** — Automated backups and **point-in-time recovery** window (days).
- **`backup_window` / `maintenance_window`** — UTC windows for backups and patching; `auto_minor_version_upgrade = true`.
- **`skip_final_snapshot` / `final_snapshot_identifier`** — On destroy, optional last snapshot; identifier includes `random_id` when snapshots are enabled.
- **`deletion_protection`** — Blocks casual delete/destroy on the instance.
- **`copy_tags_to_snapshot = true`** — Propagates tags to snapshots for cost and ownership.

### Observability

- **`enabled_cloudwatch_logs_exports`** — `postgresql` and `upgrade` logs to CloudWatch.
- **`performance_insights_enabled`** — Workload and top-SQL visibility.
- **`monitoring_interval` / `monitoring_role_arn`** — Enhanced monitoring every **60s** via the IAM role in this module.

### Fixed in HCL (not module variables)

- Backup and maintenance windows are hard-coded in `main.tf`.
- No custom **DB parameter group**, **RDS Proxy**, or **read replicas** — single primary (+ Multi-AZ standby when enabled), one endpoint for apps.

## Module outputs

| Output | Use |
| --- | --- |
| `endpoint` | Host and port (`hostname:port`) |
| `address` | Hostname without port (used by `module.secrets`) |
| `port` | Listener port (PostgreSQL default unless changed) |
| `db_name` | Created database name |
| `identifier` | AWS instance identifier (CLI/support; not the JDBC connection string) |

## Source files

- [`main.tf`](main.tf) — Resources above
- [`variables.tf`](variables.tf) — Module inputs and defaults
- [`outputs.tf`](outputs.tf) — Connection and identity outputs

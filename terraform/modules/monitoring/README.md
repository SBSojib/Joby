# Monitoring module

Terraform module that provisions **operational visibility** for Joby in Amazon CloudWatch: a shared operations dashboard, log-based application error and warning metrics, and alarms on RDS, EKS, optional WAF, and those custom log metrics. Optional **Amazon SNS** email notifications attach when alerting is enabled and an address is supplied.

Root wiring lives in [`terraform/main.tf`](../../main.tf) (`module "monitoring"`). Identifiers for RDS, EKS, the application log group, and regional WAF come from other modules and a root log group resource; this module does not deploy workloads or ingest logs by itself.

## Resources in this module

The module defines **thirteen Terraform constructs** (eleven AWS resources, one locals block, no data sources). SNS resources are created only when alerting is enabled; the WAF alarm is created only when a Web ACL name is non-empty.

| Resource / data source | What it is | Purpose | How it connects |
| --- | --- | --- | --- |
| `locals` (`name_prefix`, `alarm_actions`, `ok_actions`, `custom_namespace`, `dashboard_name`, `create_subscription`) | Terraform locals | Naming prefix, **SNS ARNs** for alarm actions (or empty when alerting is off), custom metric namespace, dashboard name, and whether to create an email subscription | `alarm_actions` / `ok_actions` feed all `aws_cloudwatch_metric_alarm` resources; `custom_namespace` is used by log metric filters and related alarms; `dashboard_name` is used by `aws_cloudwatch_dashboard.operations` |
| `aws_sns_topic.alerts` | SNS topic | **Notification bus** for alarm state changes | Created when `enable_alerting` is true; ARN referenced by `local.alarm_actions` and `aws_sns_topic_subscription.email` |
| `aws_sns_topic_subscription.email` | SNS email subscription | Delivers alarm notifications to **`alert_email`** | Created when alerting is on and `alert_email` is non-empty after trim; subscribes to `aws_sns_topic.alerts[0]` |
| `aws_cloudwatch_log_metric_filter.app_errors` | CloudWatch Logs metric filter | Increments **`AppErrorCount`** when log events match pattern `ERROR` | Reads `var.application_log_group_name` (root `aws_cloudwatch_log_group.app.name`); metric namespace `local.custom_namespace` |
| `aws_cloudwatch_log_metric_filter.app_warnings` | CloudWatch Logs metric filter | Increments **`AppWarningCount`** when log events match pattern `WARN` | Same log group input as `app_errors`; feeds `aws_cloudwatch_metric_alarm.app_warnings_high` |
| `aws_cloudwatch_metric_alarm.rds_cpu_high` | CloudWatch alarm | Fires when **RDS CPU** average exceeds **80%** over two 5-minute periods | Dimension `DBInstanceIdentifier` = `var.rds_instance_identifier` (`module.rds.identifier`); namespace `AWS/RDS` |
| `aws_cloudwatch_metric_alarm.rds_free_storage_low` | CloudWatch alarm | Fires when **RDS free storage** average falls below **5 GiB** | Same RDS dimension as CPU alarm; threshold **5368709120** bytes |
| `aws_cloudwatch_metric_alarm.rds_connections_high` | CloudWatch alarm | Fires when **RDS connection count** average exceeds **100** | Same RDS dimension; namespace `AWS/RDS` |
| `aws_cloudwatch_metric_alarm.eks_failed_requests` | CloudWatch alarm | Fires when **EKS API failed request** sum is above zero | Dimension `ClusterName` = `var.eks_cluster_name` (`module.eks.cluster_name`); `treat_missing_data = notBreaching` |
| `aws_cloudwatch_metric_alarm.app_errors` | CloudWatch alarm | Fires when **`AppErrorCount`** sum is above zero | Metric from `aws_cloudwatch_log_metric_filter.app_errors` in `local.custom_namespace` |
| `aws_cloudwatch_metric_alarm.app_warnings_high` | CloudWatch alarm | Fires when **`AppWarningCount`** sum exceeds **25** in two 5-minute periods | Metric from `aws_cloudwatch_log_metric_filter.app_warnings` |
| `aws_cloudwatch_metric_alarm.waf_blocked_requests` | CloudWatch alarm | Fires when **WAF blocked requests** sum reaches **100** or more in one period | Created when `var.waf_web_acl_name` is non-empty (`module.edge.waf_web_acl_name`); dimensions `WebACL`, `Region` (`var.aws_region`), `Rule = ALL` |
| `aws_cloudwatch_dashboard.operations` | CloudWatch dashboard | Single **operations** view of RDS CPU, connections, free storage, EKS API failures, and app errors | Widget metrics use `var.rds_instance_identifier`, `var.eks_cluster_name`, `var.aws_region`, and `AppErrorCount` in `local.custom_namespace` |

## How this module connects to the rest of the stack

**Inputs from other modules and root (`module "monitoring"` in [`main.tf`](../../main.tf)):**

- `rds_instance_identifier` from `module.rds.identifier` (RDS must exist so `AWS/RDS` metrics and alarms target the live instance).
- `eks_cluster_name` from `module.eks.cluster_name` (EKS control plane metrics).
- `application_log_group_name` from root [`aws_cloudwatch_log_group.app`](../../main.tf) (`/${project_name}/${environment}`, 7-day retention at root).
- `waf_web_acl_name` from `module.edge.waf_web_acl_name` (regional WAF Web ACL **name** for `AWS/WAFV2` alarm dimensions).
- `alert_email` and `enable_alerting` from root `monitoring_alert_email` and `enable_monitoring_alerting`.
- `project_name`, `environment`, `aws_region`, and `tags` from root variables and `local.common_tags`.

**Not defined inside this module:**

- Root **`aws_cloudwatch_log_group.app`** creates the log group that metric filters watch; applications or agents must **write** to that group name for error and warning alarms to reflect real traffic.
- [`module.rds`](../rds/main.tf) publishes RDS and enhanced-monitoring metrics; this module only **consumes** the instance identifier.
- [`module.eks`](../eks/main.tf) and the EKS control plane publish `AWS/EKS` metrics.
- [`module.edge`](../edge/main.tf) provisions WAF; blocked-request metrics appear when the Web ACL name is passed and traffic hits WAF.
- [`module.eks_addons`](../eks_addons/main.tf) may ship container logs to a **different** CloudWatch log group (`/aws/eks/<cluster_name>/application` via Fluent Bit) than the root app log group used here.
- [`module.cost_controls`](../cost_controls/main.tf) handles **billing** budgets and cost anomaly detection separately; it can reuse the monitoring alert email at root but is not part of this module.

**Downstream consumers:**

- Root outputs `monitoring_dashboard_name` and `monitoring_alerts_topic_arn` for operators and runbooks.
- No other Terraform modules depend on this module’s outputs in the current root stack.

**Typical apply order:** RDS and EKS (and edge WAF when used) → root app log group → `module.monitoring` (filters, alarms, dashboard, optional SNS). Confirm SNS email subscriptions in the inbox when email alerting is enabled. Alarms evaluate published metrics even when `enable_alerting` is false; only SNS actions are omitted.

## Notable parameters

### Alerting and notifications

- **`enable_alerting`** (variable, default **`true`**) — When false, **no SNS topic** is created and alarms have empty `alarm_actions` / `ok_actions` (CloudWatch still shows alarm state in the console).
- **`alert_email`** (variable, default **`""`**) — Email subscription is created only when alerting is on and the address is non-empty; recipients must **confirm** the SNS subscription before mail is delivered.
- **Root `enable_monitoring_alerting` / `monitoring_alert_email`** — Same behavior at apply time; see [`monitoring.auto.tfvars.example`](../../monitoring.auto.tfvars.example).

### Log-based application metrics

- **Filter patterns `ERROR` and `WARN`** — Hard-coded substring matches on `var.application_log_group_name`; not full structured logging or JSON field filters.
- **`local.custom_namespace`** — `${project_name}/${environment}` for custom metrics (`AppErrorCount`, `AppWarningCount`).
- **App error alarm** — Sum above 0 in one 5-minute period; **warning alarm** — sum above 25 over two periods.

### RDS, EKS, and WAF alarms

- **RDS thresholds** — CPU **80%**, free storage **5 GiB**, connections **100**; **300s** periods, mostly **2** evaluation periods (EKS and some app/WAF alarms use **1**).
- **`treat_missing_data = notBreaching`** — On EKS failed requests, app log metrics, and WAF blocked requests (avoids false alarms when metrics are absent).
- **`waf_web_acl_name`** (variable, default **`""`**) — Empty name **skips** the WAF alarm (`count = 0`); edge must expose a regional Web ACL name for blocked-request monitoring.

### Dashboard

- **`local.dashboard_name`** — `${project_name}-${environment}-operations`; widgets cover RDS CPU, connections, free storage, EKS API failures, and app errors only (no WAF or warning widgets in the dashboard body).

### Fixed in HCL (not module variables)

- Alarm thresholds, evaluation periods, statistics, metric namespaces, log filter patterns, dashboard layout, and WAF dimension `Rule = ALL`.
- No CloudWatch **composite** alarms, **anomaly detection** bands, or **cross-account** observability configuration in this module.

## Module outputs

| Output | Use |
| --- | --- |
| `dashboard_name` | Root output `monitoring_dashboard_name`; open the operations dashboard in CloudWatch |
| `alerts_topic_arn` | Root output `monitoring_alerts_topic_arn`; SNS topic for alarm actions when alerting is enabled (`null` when disabled) |

## Source files

- [`main.tf`](main.tf) — Locals, SNS, log metric filters, alarms, dashboard
- [`variables.tf`](variables.tf) — Module inputs and defaults
- [`outputs.tf`](outputs.tf) — Dashboard name and alerts topic ARN

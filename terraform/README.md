# Joby AWS Terraform

This stack provisions the AWS foundation for running Joby on EKS with managed AWS services around it:

- Dedicated VPC with public and private subnets across multiple Availability Zones
- Route 53 public hosted zone, CloudFront, ACM certificates, and WAF Web ACLs for the public application edge
- EKS cluster with managed node group, control-plane logs, managed core add-ons, and OIDC for IRSA
- Terraform-managed Helm add-ons for AWS Load Balancer Controller, External Secrets Operator, ExternalDNS, metrics-server, cluster-autoscaler, Fluent Bit, and ADOT telemetry
- ECR repositories for backend and frontend images
- Private RDS PostgreSQL
- Private S3 uploads bucket
- AWS Secrets Manager for runtime secrets
- CloudWatch logs, X-Ray/ADOT telemetry, alarms, dashboard, and optional SNS email alerts
- AWS Budgets and Cost Anomaly Detection for billing guardrails
- CloudTrail, GuardDuty, AWS Config, and Security Hub baseline
- Least-privilege IAM role for the backend Kubernetes service account to access the uploads bucket

## Architecture

Joby uses a standard production web application layout: public entry points, private compute, private data services, and managed control planes.

Traffic should enter through an AWS load balancer in the public subnets and then reach Kubernetes services running on EKS worker nodes in private subnets. The database and upload storage are not publicly reachable. Worker nodes use NAT gateway egress for image pulls and AWS API calls unless you add VPC endpoints later.

The Terraform modules are intentionally small and service-oriented:

- `network`: owns the VPC, subnets, internet gateway, NAT gateways, route tables, VPC endpoints, and Kubernetes subnet discovery tags.
- `edge`: owns Route 53, CloudFront, ACM certificate validation, and WAF rules for the public application endpoint.
- `eks`: owns the EKS control plane, node role, managed node group, managed add-ons, OIDC provider, and node security group.
- `eks_addons`: owns Terraform-managed Helm add-ons and their IRSA roles.
- `rds`: owns PostgreSQL, backups, encryption, final snapshot behavior, and deletion protection.
- `s3`: owns the uploads bucket, public access block, encryption, ownership controls, TLS-only access policy, versioning, and lifecycle cleanup.
- `ecr`: owns container repositories, scan-on-push, encryption, and cleanup of untagged images.
- `irsa`: owns the IAM role trusted by the backend Kubernetes service account.
- `secrets`: owns the Secrets Manager application secret shell (values managed outside Terraform).
- `security_baseline`: owns CloudTrail, GuardDuty, AWS Config, Security Hub, and the audit log bucket.
- `security`: owns application and database security groups.
- `cost_controls`: owns AWS Budgets and Cost Anomaly Detection.
- `logging` and `monitoring`: own CloudWatch logs, alarms, dashboard, and optional SNS alerting.

## Architectural Decisions

### Dedicated VPC Instead Of Default VPC

The stack creates a dedicated VPC instead of using the AWS default VPC. Real production accounts commonly host several systems, shared experiments, and legacy resources; relying on the default VPC makes network boundaries unclear and can accidentally expose workloads. A dedicated VPC gives Joby its own CIDR, route tables, subnet tags, and security groups.

### Public And Private Subnets

Public subnets are reserved for internet-facing infrastructure such as load balancers and NAT gateways. EKS nodes and RDS run in private subnets across three Availability Zones. This is the common AWS pattern for customer-facing applications because only the edge layer needs public routing; application compute and databases should not have direct public IP exposure.

### NAT Gateway Egress

Private subnets use NAT gateway egress with one NAT gateway per Availability Zone (three AZs). This lets EKS nodes pull container images, download OS packages, and call external APIs without being directly reachable from the internet.

### VPC Endpoints

The stack creates private endpoints for S3, ECR, CloudWatch Logs, STS, Secrets Manager, and SSM-related services. This reduces dependency on NAT gateways for AWS API traffic and keeps common control-plane calls on the AWS network. NAT remains useful for non-AWS internet egress.

### CloudFront, ALB, ACM, Route 53, And WAF

The public application edge uses Route 53 for DNS, CloudFront for global edge delivery, ACM for TLS, AWS Load Balancer Controller for ALB provisioning, and WAF managed rules for common web attacks and rate limiting. CloudFront serves `app.<domain>` while the ALB is exposed through `origin.app.<domain>` for the distribution origin. This keeps the user-facing endpoint at the edge while still allowing Kubernetes to own service routing.

### EKS Managed Node Group

The cluster uses EKS managed node groups instead of self-managed nodes. Managed node groups reduce operational work for node lifecycle, draining, replacement, and Kubernetes version compatibility. The node group uses a launch template so the Terraform-managed node security group is actually attached to the nodes, which keeps the RDS ingress rule precise.

### EKS API Endpoint Access

The EKS API endpoint always has private access enabled. Public access is optional via `eks_cluster_endpoint_public_access`; when enabled, restrict `eks_cluster_public_access_cidrs` to trusted office/VPN/CI CIDRs. Terraform-managed Helm add-ons require API reachability during `terraform apply`.

### EKS Managed Add-ons And OIDC

Core add-ons (`vpc-cni`, `kube-proxy`, and `coredns`) are managed through EKS so cluster-critical components are visible in Terraform and can be upgraded deliberately. The cluster also creates an OIDC provider for IAM Roles for Service Accounts. This is the AWS-recommended way for pods to access AWS services without static access keys in Kubernetes secrets.

### Backend IRSA Role For S3

The backend gets a dedicated IAM role that trusts only one Kubernetes service account: `system:serviceaccount:<namespace>:<service-account>`. That role can list the uploads bucket and manage objects inside it. In real applications, this avoids long-lived AWS keys in app configuration and limits the blast radius if a pod is compromised.

### Secrets Manager And External Secrets

Runtime secrets are stored in AWS Secrets Manager and synced into Kubernetes by External Secrets Operator. Terraform creates the secret shell only; populate secret values in the AWS console, CLI, or your secret-management process before deploying the application.

### Private RDS PostgreSQL

RDS is private, encrypted, Multi-AZ, deletion-protected, and reachable only from approved application security groups. Final snapshots are always taken on destroy. Backup retention is configurable via `db_backup_retention_period`.

### S3 Upload Bucket Hardening

The uploads bucket blocks public access, enforces bucket-owner ownership, uses server-side encryption, denies non-TLS requests, and enables versioning by default. This matches the common pattern for user-uploaded content where accidental public exposure and accidental deletion are bigger risks than raw storage cost. Lifecycle cleanup limits long-term version buildup.

### ECR Repository Controls

ECR repositories scan images on push, use encryption, expire untagged images, and use immutable tags. The deploy workflow uses commit SHA tags and fails on high or critical ECR scan findings before rolling out to EKS.

### Security Baseline

CloudTrail records account API activity, GuardDuty monitors threat signals, AWS Config records resource configuration history, and Security Hub enables centralized posture findings. These services are account-level controls and should stay enabled in production accounts.

### Telemetry

The EKS add-on stack installs ADOT and CloudWatch observability components so application traces and container metrics can flow to AWS-native telemetry services. The backend deployment includes OpenTelemetry environment settings and is ready to export to the in-cluster ADOT collector.

### Cost Controls

AWS Budgets alerts on forecasted and actual monthly spend, while Cost Anomaly Detection watches service-level spend changes. Both use the billing alert email when configured, falling back to the monitoring alert email.

### CloudWatch Monitoring

The stack creates a CloudWatch dashboard and alarms for RDS health, EKS API failures, WAF blocks, and application warning/error logs. SNS email subscriptions are optional when `monitoring_alert_email` is set (recipients must confirm the subscription).

## Prerequisites

- Terraform >= 1.5
- AWS CLI configured with access to the target account
- Docker for building and pushing images

## Terraform State

This stack uses **local state** only (`terraform.tfstate` in the `terraform/` directory). Do not commit state files; they may contain sensitive values.

```bash
cd terraform
terraform init
```

Keep `.terraform.lock.hcl` committed so provider versions are reproducible in CI and on developer machines.

## Variable Files

Use split variable files in `terraform/`:

- `general.auto.tfvars`: region, environment, VPC, subnet, Kubernetes namespace, and alert emails
- `eks.auto.tfvars`: cluster version, node group size, and optional public API access
- `rds.auto.tfvars`: PostgreSQL sizing and backup retention
- `s3.auto.tfvars`: uploads bucket name

Templates are included as `*.auto.tfvars.example`.

## Environment Guidance

Fixed infrastructure choices: three Availability Zones, one NAT gateway per AZ, Multi-AZ RDS with deletion protection and final snapshots, private EKS API access, always-on Helm add-ons and CloudWatch alarms.

To reduce cost in non-production environments, lower RDS/EKS sizing and set `monthly_budget_limit_usd` with `billing_alert_email` or `monitoring_alert_email` for cost alerts.

## Provision

```bash
cd terraform
terraform init
terraform fmt -recursive
terraform validate
terraform plan
terraform apply
```

Ensure Terraform can reach the EKS API during apply (for example, set `eks_cluster_endpoint_public_access = true` with restricted CIDRs for bootstrap from a laptop, or run apply from CI inside the network).

After apply, populate the Secrets Manager secret named in output `application_secret_name` before deploying workloads.

## Key Outputs

- `vpc_id`
- `route53_name_servers`
- `app_hostname`
- `origin_hostname`
- `cloudfront_distribution_id`
- `cloudfront_distribution_domain_name`
- `acm_certificate_arn`
- `waf_web_acl_arn`
- `private_subnet_ids`
- `public_subnet_ids`
- `eks_cluster_name`
- `eks_cluster_endpoint`
- `eks_oidc_provider_arn`
- `backend_irsa_role_arn`
- `ecr_backend_repository_url`
- `ecr_frontend_repository_url`
- `rds_address`
- `s3_bucket_name`
- `application_secret_name`
- `monthly_budget_name`
- `cost_anomaly_monitor_arn`
- `monitoring_dashboard_name`

## Deploy Joby To EKS

Use `k8s/eks/README.md` for the deployment sequence.

The EKS manifests include placeholders for `backend_irsa_role_arn`, `application_secret_name`, `origin_hostname`, `acm_certificate_arn`, and `waf_web_acl_arn`. The GitHub deploy workflow renders those values from repository variables populated from Terraform outputs. Users access `app_hostname`, which resolves to CloudFront.

# Joby AWS Terraform

This stack provisions the AWS foundation for running Joby on EKS with managed AWS services around it:

- Dedicated VPC with public and private subnets across multiple Availability Zones
- Route 53 public hosted zone, ACM certificate, and WAF Web ACL for the public application edge
- EKS cluster with managed node group, control-plane logs, managed core add-ons, and OIDC for IRSA
- Terraform-managed Helm add-ons for AWS Load Balancer Controller, External Secrets Operator, ExternalDNS, metrics-server, cluster-autoscaler, and Fluent Bit
- ECR repositories for backend and frontend images
- Private RDS PostgreSQL
- Private S3 uploads bucket
- AWS Secrets Manager for runtime secrets
- CloudWatch logs, alarms, dashboard, and optional SNS email alerts
- CloudTrail, GuardDuty, AWS Config, and Security Hub baseline
- Least-privilege IAM role for the backend Kubernetes service account to access the uploads bucket
- Optional EC2 host for legacy Docker Compose deployments (`provision_ec2 = true`)

## Architecture

Joby uses a standard production web application layout: public entry points, private compute, private data services, and managed control planes.

Traffic should enter through an AWS load balancer in the public subnets and then reach Kubernetes services running on EKS worker nodes in private subnets. The database and upload storage are not publicly reachable. Worker nodes use NAT gateway egress for image pulls and AWS API calls unless you add VPC endpoints later.

The Terraform modules are intentionally small and service-oriented:

- `network`: owns the VPC, subnets, internet gateway, NAT gateways, route tables, VPC endpoints, and Kubernetes subnet discovery tags.
- `edge`: owns Route 53, ACM certificate validation, and WAF rules for the public application endpoint.
- `eks`: owns the EKS control plane, node role, managed node group, managed add-ons, OIDC provider, and node security group.
- `eks_addons`: owns Terraform-managed Helm add-ons and their IRSA roles.
- `rds`: owns PostgreSQL, backups, encryption, final snapshot behavior, and deletion protection.
- `s3`: owns the uploads bucket, public access block, encryption, ownership controls, TLS-only access policy, versioning, and lifecycle cleanup.
- `ecr`: owns container repositories, scan-on-push, encryption, and cleanup of untagged images.
- `irsa`: owns the IAM role trusted by the backend Kubernetes service account.
- `secrets`: owns the Secrets Manager application secret metadata and optional managed secret value.
- `security_baseline`: owns CloudTrail, GuardDuty, AWS Config, Security Hub, and the audit log bucket.
- `security`: owns application and database security groups.
- `logging` and `monitoring`: own CloudWatch logs, alarms, dashboard, and optional SNS alerting.

## Architectural Decisions

### Dedicated VPC Instead Of Default VPC

The stack creates a dedicated VPC instead of using the AWS default VPC. Real production accounts commonly host several systems, shared experiments, and legacy resources; relying on the default VPC makes network boundaries unclear and can accidentally expose workloads. A dedicated VPC gives Joby its own CIDR, route tables, subnet tags, and security groups.

### Public And Private Subnets

Public subnets are reserved for internet-facing infrastructure such as load balancers, NAT gateways, and the optional EC2 legacy host. EKS nodes and RDS run in private subnets. This is the common AWS pattern for customer-facing applications because only the edge layer needs public routing; application compute and databases should not have direct public IP exposure.

### NAT Gateway Egress

Private subnets use NAT gateway egress by default. This lets EKS nodes pull container images, download OS packages, and call external APIs without being directly reachable from the internet. Production defaults use one NAT gateway per Availability Zone for fault isolation.

### VPC Endpoints

The stack creates private endpoints for S3, ECR, CloudWatch Logs, STS, Secrets Manager, and SSM-related services. This reduces dependency on NAT gateways for AWS API traffic and keeps common control-plane calls on the AWS network. NAT remains useful for non-AWS internet egress.

### ALB, ACM, Route 53, And WAF

The public application edge uses Route 53 for DNS, ACM for TLS, AWS Load Balancer Controller for ALB provisioning, and WAF managed rules for common web attacks and rate limiting. This is the standard AWS pattern for public Kubernetes web applications because Kubernetes owns service routing while AWS owns edge TLS, DNS, and threat filtering.

### EKS Managed Node Group

The cluster uses EKS managed node groups instead of self-managed nodes. Managed node groups reduce operational work for node lifecycle, draining, replacement, and Kubernetes version compatibility. The node group uses a launch template so the Terraform-managed node security group is actually attached to the nodes, which keeps the RDS ingress rule precise.

### EKS API Endpoint Access

The EKS API endpoint supports both public and private access by default. Private access allows in-VPC operations, while public access keeps first deployments practical from a developer workstation or CI runner. In production, restrict `eks_cluster_public_access_cidrs` to trusted office/VPN/CI CIDRs instead of leaving `0.0.0.0/0`.

### EKS Managed Add-ons And OIDC

Core add-ons (`vpc-cni`, `kube-proxy`, and `coredns`) are managed through EKS so cluster-critical components are visible in Terraform and can be upgraded deliberately. The cluster also creates an OIDC provider for IAM Roles for Service Accounts. This is the AWS-recommended way for pods to access AWS services without static access keys in Kubernetes secrets.

### Backend IRSA Role For S3

The backend gets a dedicated IAM role that trusts only one Kubernetes service account: `system:serviceaccount:<namespace>:<service-account>`. That role can list the uploads bucket and manage objects inside it. In real applications, this avoids long-lived AWS keys in app configuration and limits the blast radius if a pod is compromised.

### Secrets Manager And External Secrets

Runtime secrets are stored in AWS Secrets Manager and synced into Kubernetes by External Secrets Operator. This avoids committing secret YAML or writing secrets from CI. Terraform can optionally manage the initial secret value with `manage_application_secret_value`, but for mature production environments you can leave values managed by a controlled secret-rotation process.

### Private RDS PostgreSQL

RDS is private, encrypted, tagged, and reachable only from approved application security groups. Backups, final snapshot behavior, deletion protection, Multi-AZ, and Performance Insights are variables because dev and production have different cost and recovery needs. For production, use Multi-AZ, deletion protection, nonzero backup retention, and final snapshots.

### S3 Upload Bucket Hardening

The uploads bucket blocks public access, enforces bucket-owner ownership, uses server-side encryption, denies non-TLS requests, and enables versioning by default. This matches the common pattern for user-uploaded content where accidental public exposure and accidental deletion are bigger risks than raw storage cost. Lifecycle cleanup limits long-term version buildup.

### ECR Repository Controls

ECR repositories scan images on push, use encryption, expire untagged images, and use immutable tags. The deploy workflow uses commit SHA tags and fails on high or critical ECR scan findings before rolling out to EKS.

### Security Baseline

CloudTrail records account API activity, GuardDuty monitors threat signals, AWS Config records resource configuration history, and Security Hub enables centralized posture findings. These services are account-level controls and should stay enabled in production accounts.

### CloudWatch Monitoring

The stack creates a CloudWatch dashboard and alarms for RDS health, EKS API failures, and application error logs. This gives a small team immediate operational visibility without introducing another monitoring platform. SNS email alerts are optional because email subscriptions require recipient confirmation.

### Optional EC2 Legacy Mode

`provision_ec2` remains available for a legacy Docker Compose deployment path. It is isolated from the main EKS path and placed in a public subnet only when enabled. New deployments should prefer EKS; the EC2 path is useful for migration, demos, or simple break-glass testing.

## Prerequisites

- Terraform >= 1.5
- AWS CLI configured with access to the target account
- Docker for building and pushing images
- Optional S3 bucket for Terraform remote state
- Optional DynamoDB table for Terraform remote state locking

## Terraform State

Terraform backend configuration is intentionally opt-in. Backend selection happens during `terraform init`, before Terraform variables are loaded, so it cannot be controlled with a normal `*.tfvars` setting.

### Local State

Local state is the default. Use it for local experiments, learning, and short-lived dev environments where you do not need team state sharing:

```bash
cd terraform
terraform init
```

Or run the helper:

```bash
bash ./init-local.sh
```

This path does not require an S3 backend bucket and will not prompt for one.

### Remote S3 State

Use remote S3 state for shared environments such as staging and production. Remote state gives the team a single source of truth and supports state locking when a DynamoDB lock table is provided.

The remote backend does not create the S3 bucket or DynamoDB table. Create those bootstrap resources once outside this stack, then initialize with:

```bash
cd terraform
bash ./init-remote-s3.sh \
  -b <tf-state-bucket> \
  -r <aws-region> \
  -k joby/dev/terraform.tfstate
```

Add `-d <tf-lock-table>` if you use a DynamoDB lock table.

PowerShell helpers are also available:

```powershell
.\init-local.ps1
.\init-remote-s3.ps1 -Bucket <tf-state-bucket> -Region <aws-region> -Key joby/dev/terraform.tfstate
```

The remote helper creates `backend.generated.tf` from `backend.generated.tf.example`. That generated file is ignored by Git so local and remote workflows do not fight each other.

Keep `.terraform.lock.hcl` committed so provider versions are reproducible in CI and on developer machines.

## Variable Files

Use split variable files in `terraform/`:

- `general.auto.tfvars`: region, environment, VPC, subnet, and Kubernetes namespace settings
- `eks.auto.tfvars`: cluster version, node group size, and API endpoint access
- `rds.auto.tfvars`: PostgreSQL sizing, backups, deletion protection, and HA controls
- `s3.auto.tfvars`: uploads bucket name, versioning, and destroy behavior
- `ec2.auto.tfvars`: optional legacy EC2 mode
- `monitoring.auto.tfvars`: alerting toggle and email subscription

Templates are included as `*.auto.tfvars.example`.

## Environment Guidance

The defaults now favor production: three Availability Zones, one NAT gateway per AZ, VPC endpoints, Multi-AZ RDS, deletion protection, final snapshots, seven-day backup retention, immutable image tags, and account security services enabled.

For temporary development environments, reduce cost explicitly in your var files:

- Set `az_count = 2` and `single_nat_gateway = true`.
- Set `db_multi_az = false`, `db_deletion_protection = false`, `db_skip_final_snapshot = true`, and lower RDS sizing.
- Set `enable_security_baseline = false` only for disposable sandboxes.
- Leave `enable_kubernetes_addons = false` until the EKS cluster has been created once.

## Provision

```bash
cd terraform
terraform init
terraform fmt -recursive
terraform validate
terraform plan
terraform apply
```

For Terraform-managed Helm/Kubernetes add-ons, use a two-step bootstrap:

```bash
terraform apply -var="enable_kubernetes_addons=false"
terraform apply -var="enable_kubernetes_addons=true"
```

The first apply creates the EKS API endpoint. The second apply lets the Helm provider connect to the cluster and install controllers/operators.

## Key Outputs

- `vpc_id`
- `route53_name_servers`
- `app_hostname`
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
- `monitoring_dashboard_name`

## Deploy Joby To EKS

Use `k8s/eks/README.md` for the deployment sequence.

The EKS manifests include placeholders for `backend_irsa_role_arn`, `application_secret_name`, `app_hostname`, `acm_certificate_arn`, and `waf_web_acl_arn`. The GitHub deploy workflow renders those values from repository variables populated from Terraform outputs.

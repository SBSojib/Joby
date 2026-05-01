# Joby Terraform

This stack provisions AWS infrastructure for deploying Joby on EKS:

- EKS cluster with managed node group
- ECR repositories for backend/frontend images
- RDS PostgreSQL
- S3 uploads bucket
- CloudWatch log group
- Security groups (RDS allows EKS node access)
- Optional EC2 host for legacy Docker Compose (`provision_ec2 = true`)

## Prerequisites

- Terraform >= 1.5
- AWS CLI configured
- Docker (for building/pushing images)

## Variable files

Use split var files in `terraform/`:

- `general.auto.tfvars`
- `eks.auto.tfvars`
- `rds.auto.tfvars`
- `s3.auto.tfvars`
- `ec2.auto.tfvars` (optional EC2 mode)
- `monitoring.auto.tfvars`

Templates:

- `general.auto.tfvars.example`
- `eks.auto.tfvars.example`
- `rds.auto.tfvars.example`
- `s3.auto.tfvars.example`
- `ec2.auto.tfvars.example`
- `monitoring.auto.tfvars.example`

## Provision

```bash
cd terraform
terraform init
terraform plan
terraform apply
```

## Key outputs

- `eks_cluster_name`
- `eks_cluster_endpoint`
- `ecr_backend_repository_url`
- `ecr_frontend_repository_url`
- `rds_address`
- `s3_bucket_name`
- `monitoring_dashboard_name`

## Deploy Joby to EKS

Use `k8s/eks/README.md` for the deployment sequence.

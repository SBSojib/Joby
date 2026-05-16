# GitHub CI/CD

Joby uses GitHub Actions for pull request validation, infrastructure changes, and EKS deployments.

## Workflows

- `CI` runs backend restore/build/test, frontend install/lint/build, Terraform validation, Kubernetes manifest rendering, and Docker image builds.
- `Terraform` is manually triggered. It plans by default and applies only when the `apply` input is enabled.
- `Deploy to AWS` runs after a successful `CI` workflow on `main`, or manually from `workflow_dispatch`. It builds backend/frontend images with immutable commit SHA tags, pushes them to ECR, checks ECR scan findings, renders Kubernetes manifests, and rolls the app out to EKS.

## Required GitHub Secrets

- `AWS_ROLE_TO_ASSUME`: IAM role ARN trusted by GitHub OIDC.
- `DB_PASSWORD`: PostgreSQL password used by Terraform.
- `JWT_SECRET`: production JWT signing secret. Required only when Terraform manages the Secrets Manager secret value.
- `TF_STATE_BUCKET`: S3 bucket for Terraform remote state.

## Optional GitHub Secrets

- `TF_LOCK_TABLE`: DynamoDB table for Terraform state locking.

## Repository Variables

- `AWS_REGION`: defaults to `us-east-1`.
- `PROJECT_NAME`: defaults to `joby`.
- `ENVIRONMENT`: defaults to `dev`.
- `DOMAIN_NAME`: root domain for the Route 53 hosted zone, for example `example.com`.
- `APP_SUBDOMAIN`: subdomain for the application. Defaults to `app`.
- `APP_HOSTNAME`: public application hostname from Terraform output `app_hostname`.
- `ORIGIN_HOSTNAME`: ALB origin hostname from Terraform output `origin_hostname`.
- `ACM_CERTIFICATE_ARN`: ACM certificate ARN from Terraform output `acm_certificate_arn`.
- `WAF_WEB_ACL_ARN`: WAF Web ACL ARN from Terraform output `waf_web_acl_arn`.
- `BACKEND_IRSA_ROLE_ARN`: backend service account IAM role from Terraform output `backend_irsa_role_arn`.
- `APPLICATION_SECRET_NAME`: Secrets Manager name from Terraform output `application_secret_name`.
- `EKS_CLUSTER_NAME`: optional override. Defaults to `${PROJECT_NAME}-${ENVIRONMENT}-eks`.
- `ECR_BACKEND_REPOSITORY`: optional override. Defaults to `${PROJECT_NAME}-${ENVIRONMENT}-joby-backend`.
- `ECR_FRONTEND_REPOSITORY`: optional override. Defaults to `${PROJECT_NAME}-${ENVIRONMENT}-joby-frontend`.
- `S3_BUCKET_NAME`: required by the Kubernetes config map.
- `CORS_ALLOWED_ORIGIN`: public frontend origin allowed by the backend. Defaults to `https://${APP_HOSTNAME}` during deployment.
- `VITE_API_URL`: defaults to `/api`.
- `MONITORING_ALERT_EMAIL`: optional Terraform alert email.
- `BILLING_ALERT_EMAIL`: optional Terraform billing alert email. Falls back to `MONITORING_ALERT_EMAIL`.
- `MONTHLY_BUDGET_LIMIT_USD`: optional monthly budget limit. Defaults to `250`.
- `COST_ANOMALY_THRESHOLD_USD`: optional cost anomaly impact threshold. Defaults to `25`.

## AWS Permissions

The GitHub OIDC role needs permission to:

- run Terraform against this stack, including EKS, ECR, RDS, S3, IAM, CloudWatch, SNS, Route 53, ACM, CloudFront, WAF, CloudTrail, GuardDuty, Config, Security Hub, Secrets Manager, Budgets, Cost Explorer, and related network resources;
- push images to the backend and frontend ECR repositories;
- describe ECR image scan findings;
- call `eks:DescribeCluster`;
- update Kubernetes resources in the target EKS cluster.

Grant the role Kubernetes access to the cluster through EKS access entries or the cluster's `aws-auth` mapping before using the deploy workflow.

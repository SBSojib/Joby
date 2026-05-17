# GitHub CI/CD

Joby uses GitHub Actions for pull request validation and EKS deployments.

## Workflows

- `CI` runs backend restore/build/test, frontend install/lint/build, Terraform validation, Kubernetes manifest rendering, and Docker image builds.
- `Deploy to AWS` runs after a successful `CI` workflow on `main`, or manually from `workflow_dispatch`. It builds backend/frontend images with immutable commit SHA tags, pushes them to ECR, checks ECR scan findings, renders Kubernetes manifests, and rolls the app out to EKS.

Infrastructure is provisioned with Terraform locally (or another process outside GitHub Actions). Apply `terraform/` changes outside the pipeline.

## Required GitHub Secrets

- `AWS_ROLE_TO_ASSUME`: IAM role ARN trusted by GitHub OIDC.

## Repository Variables

- `AWS_REGION`: defaults to `us-east-1`.
- `PROJECT_NAME`: defaults to `joby`.
- `ENVIRONMENT`: defaults to `dev`.
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

## AWS Permissions

The GitHub OIDC role needs permission to:

- push images to the backend and frontend ECR repositories;
- describe ECR image scan findings;
- call `eks:DescribeCluster`;
- update Kubernetes resources in the target EKS cluster.

Grant the role Kubernetes access to the cluster through EKS access entries or the cluster's `aws-auth` mapping before using the deploy workflow.

# Joby on EKS

## GitHub Actions deployment

Use `.github/workflows/deploy-aws.yml` to build immutable commit-SHA images, push to ECR, check scan findings, render the ALB/ExternalSecret placeholders, and roll out the backend/frontend Deployments. Runtime secrets come from AWS Secrets Manager through External Secrets Operator; the deploy workflow no longer writes Kubernetes secrets directly.

## Manual deployment

1. Provision infrastructure from `terraform/` (`terraform apply` installs EKS, Helm add-ons, and supporting AWS resources in one run). Terraform must be able to reach the EKS API during apply; enable `eks_cluster_endpoint_public_access` temporarily or run apply from a networked environment if the API is private-only.
2. Populate the Secrets Manager secret shell (Terraform output `application_secret_name`) with runtime values before deploying the app.
3. Configure kubeconfig:
   `aws eks update-kubeconfig --region <aws-region> --name <eks-cluster-name>`
4. Login to ECR:
   `aws ecr get-login-password --region <aws-region> | docker login --username AWS --password-stdin <aws-account-id>.dkr.ecr.<aws-region>.amazonaws.com`
5. Build and push images with an immutable tag:
   - `IMAGE_TAG=$(git rev-parse HEAD)`
   - backend: `docker build -t <backend-ecr-url>:$IMAGE_TAG ./backend && docker push <backend-ecr-url>:$IMAGE_TAG`
   - frontend: `docker build -t <frontend-ecr-url>:$IMAGE_TAG ./frontend && docker push <frontend-ecr-url>:$IMAGE_TAG`
6. Update:
   - `configmap.yaml` with `S3_BUCKET_NAME`, `AWS_REGION`, `Cors__AllowedOrigins__0`
   - `serviceaccount.yaml` with `backend_irsa_role_arn`
   - `clustersecretstore.yaml` with the AWS region
   - `externalsecret.yaml` with `application_secret_name`
   - `ingress.yaml` with `origin_hostname`, `acm_certificate_arn`, and `waf_web_acl_arn`
   - `backend.yaml` image tag
   - `frontend.yaml` image tag
7. Deploy:
   - `kubectl apply -k k8s/eks`
8. Monitor:
   - Container logs: Terraform Helm **aws-for-fluent-bit** → CloudWatch log group `/aws/eks/<cluster-name>/application` (no separate manifest under `k8s/eks/`)
   - Open CloudWatch dashboard from Terraform output `monitoring_dashboard_name`
   - Confirm SNS email subscription if `monitoring_alert_email` is set
   - Confirm budget and cost anomaly email subscriptions if `billing_alert_email` is set
   - Confirm Route 53 name servers are configured at the domain registrar
   - Confirm `app_hostname` resolves to CloudFront and `origin_hostname` resolves to the ALB

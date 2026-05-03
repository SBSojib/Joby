# Joby on EKS

## GitHub Actions deployment

Use `.github/workflows/deploy-aws.yml` to build immutable commit-SHA images, push to ECR, check scan findings, render the ALB/ExternalSecret placeholders, and roll out the backend/frontend Deployments. Runtime secrets come from AWS Secrets Manager through External Secrets Operator; the deploy workflow no longer writes Kubernetes secrets directly.

## Manual deployment

1. Provision infrastructure from `terraform/`.
   - First apply with `enable_kubernetes_addons = false` creates AWS/EKS foundations.
   - Second apply with `enable_kubernetes_addons = true` installs AWS Load Balancer Controller, External Secrets Operator, ExternalDNS, metrics-server, cluster-autoscaler, and Fluent Bit.
2. Configure kubeconfig:
   `aws eks update-kubeconfig --region <aws-region> --name <eks-cluster-name>`
3. Login to ECR:
   `aws ecr get-login-password --region <aws-region> | docker login --username AWS --password-stdin <aws-account-id>.dkr.ecr.<aws-region>.amazonaws.com`
4. Build and push images with an immutable tag:
   - `IMAGE_TAG=$(git rev-parse HEAD)`
   - backend: `docker build -t <backend-ecr-url>:$IMAGE_TAG ./backend && docker push <backend-ecr-url>:$IMAGE_TAG`
   - frontend: `docker build -t <frontend-ecr-url>:$IMAGE_TAG ./frontend && docker push <frontend-ecr-url>:$IMAGE_TAG`
5. Update:
   - `configmap.yaml` with `S3_BUCKET_NAME`, `AWS_REGION`, `Cors__AllowedOrigins__0`
   - `serviceaccount.yaml` with `backend_irsa_role_arn`
   - `clustersecretstore.yaml` with the AWS region
   - `externalsecret.yaml` with `application_secret_name`
   - `ingress.yaml` with `app_hostname`, `acm_certificate_arn`, and `waf_web_acl_arn`
   - `backend.yaml` image tag
   - `frontend.yaml` image tag
6. Deploy:
   - `kubectl apply -k k8s/eks`
7. Monitor:
   - Open CloudWatch dashboard from Terraform output `monitoring_dashboard_name`
   - Confirm SNS email subscription if `monitoring_alert_email` is set
   - Confirm Route 53 name servers are configured at the domain registrar

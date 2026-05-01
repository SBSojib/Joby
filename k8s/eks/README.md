# Joby on EKS

1. Provision infrastructure from `terraform/`.
2. Configure kubeconfig:
   `aws eks update-kubeconfig --region <aws-region> --name <eks-cluster-name>`
3. Login to ECR:
   `aws ecr get-login-password --region <aws-region> | docker login --username AWS --password-stdin <aws-account-id>.dkr.ecr.<aws-region>.amazonaws.com`
4. Build and push images:
   - backend: `docker build -t <backend-ecr-url>:latest ./backend && docker push <backend-ecr-url>:latest`
   - frontend: `docker build -t <frontend-ecr-url>:latest ./frontend && docker push <frontend-ecr-url>:latest`
5. Update:
   - `configmap.yaml` with `S3_BUCKET_NAME`, `AWS_REGION`, `Cors__AllowedOrigins__0`
   - `secret.example.yaml` with real DB connection string and `Jwt__Secret`, then save as `secret.yaml`
   - `backend.yaml` image
   - `frontend.yaml` image
6. Deploy:
   - `kubectl apply -f namespace.yaml`
   - `kubectl apply -f configmap.yaml`
   - `kubectl apply -f secret.yaml`
   - `kubectl apply -f backend.yaml`
   - `kubectl apply -f frontend.yaml`
7. Deploy log shipping:
   - update `k8s/eks/observability/fluent-bit.yaml` placeholders
   - `kubectl apply -f k8s/eks/observability/fluent-bit.yaml`
8. Monitor:
   - Open CloudWatch dashboard from Terraform output `monitoring_dashboard_name`
   - Confirm SNS email subscription if `monitoring_alert_email` is set

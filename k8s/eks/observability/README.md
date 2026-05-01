# EKS Observability

This deploys Fluent Bit as a DaemonSet to ship container logs to CloudWatch.

1. Open `fluent-bit.yaml`.
2. Replace:
   - `REPLACE_WITH_AWS_REGION`
   - `REPLACE_WITH_APP_LOG_GROUP_NAME` (use Terraform output `cloudwatch_log_group`)
3. Apply:
   - `kubectl apply -f k8s/eks/observability/fluent-bit.yaml`

The EKS node IAM role already includes `CloudWatchAgentServerPolicy` from Terraform.

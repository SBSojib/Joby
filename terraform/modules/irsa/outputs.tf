output "role_arn" {
  description = "IAM role ARN for the Kubernetes service account"
  value       = aws_iam_role.this.arn
}

output "service_account_subject" {
  description = "Kubernetes service account subject trusted by the role"
  value       = "system:serviceaccount:${var.namespace}:${var.service_account_name}"
}

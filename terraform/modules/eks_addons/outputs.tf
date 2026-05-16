output "aws_load_balancer_controller_role_arn" {
  description = "IRSA role ARN for AWS Load Balancer Controller"
  value       = aws_iam_role.addon["aws_load_balancer_controller"].arn
}

output "external_secrets_role_arn" {
  description = "IRSA role ARN for External Secrets Operator"
  value       = aws_iam_role.addon["external_secrets"].arn
}

output "external_dns_role_arn" {
  description = "IRSA role ARN for ExternalDNS"
  value       = aws_iam_role.addon["external_dns"].arn
}

output "cluster_secret_store_name" {
  description = "ClusterSecretStore name used by ExternalSecret resources"
  value       = "aws-secrets-manager"
}

output "adot_collector_role_arn" {
  description = "IRSA role ARN for ADOT Collector"
  value       = aws_iam_role.addon["adot_collector"].arn
}

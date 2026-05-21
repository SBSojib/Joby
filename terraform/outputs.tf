output "rds_endpoint" {
  description = "RDS connection endpoint (host:port)"
  value       = module.rds.endpoint
}

output "rds_address" {
  description = "RDS hostname (without port)"
  value       = module.rds.address
}

output "rds_port" {
  description = "RDS port number"
  value       = module.rds.port
}

output "rds_db_name" {
  description = "Name of the PostgreSQL database"
  value       = module.rds.db_name
}

output "s3_bucket_name" {
  description = "S3 bucket name for file uploads"
  value       = module.s3.bucket_name
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN"
  value       = module.s3.bucket_arn
}

output "backend_irsa_role_arn" {
  description = "IAM role ARN for the backend Kubernetes service account"
  value       = module.backend_irsa.role_arn
}

output "application_secret_name" {
  description = "Secrets Manager name used by External Secrets Operator"
  value       = module.secrets.application_secret_name
}

output "application_secret_arn" {
  description = "Secrets Manager ARN used by External Secrets Operator"
  value       = module.secrets.application_secret_arn
}

output "external_secrets_role_arn" {
  description = "IRSA role ARN for External Secrets Operator"
  value       = module.eks_addons.external_secrets_role_arn
}

output "aws_load_balancer_controller_role_arn" {
  description = "IRSA role ARN for AWS Load Balancer Controller"
  value       = module.eks_addons.aws_load_balancer_controller_role_arn
}

output "external_dns_role_arn" {
  description = "IRSA role ARN for ExternalDNS"
  value       = module.eks_addons.external_dns_role_arn
}

output "adot_collector_role_arn" {
  description = "IRSA role ARN for ADOT Collector"
  value       = module.eks_addons.adot_collector_role_arn
}

output "cloudwatch_log_group" {
  description = "CloudWatch log group name"
  value       = aws_cloudwatch_log_group.app.name
}

output "route53_hosted_zone_id" {
  description = "Route 53 public hosted zone ID"
  value       = module.edge.hosted_zone_id
}

output "route53_hosted_zone_name" {
  description = "Route 53 hosted zone DNS name (joby.hasibul.bd in subdomain delegation mode)"
  value       = module.edge.hosted_zone_name
}

output "route53_name_servers" {
  description = "Route 53 nameservers for the hosted zone"
  value       = module.edge.hosted_zone_name_servers
}

output "cloudflare_delegation_record_name" {
  description = "Cloudflare DNS record name (label only) for NS delegation to Route 53"
  value       = var.app_subdomain != "" ? var.app_subdomain : "@"
}

output "dns_delegation_mode" {
  description = "Configured DNS delegation mode (apex or subdomain)"
  value       = var.dns_delegation_mode
}

output "app_hostname" {
  description = "Public application hostname"
  value       = module.edge.app_hostname
}

output "origin_hostname" {
  description = "ALB Ingress hostname (ExternalDNS); same as app_hostname when traffic goes directly to the load balancer"
  value       = module.edge.app_hostname
}

output "acm_certificate_arn" {
  description = "ACM certificate ARN for ALB HTTPS"
  value       = module.edge.certificate_arn
}

output "waf_web_acl_arn" {
  description = "Regional WAF Web ACL ARN for ALB"
  value       = module.edge.waf_web_acl_arn
}

output "vpc_id" {
  description = "Dedicated VPC ID"
  value       = module.network.vpc_id
}

output "private_subnet_ids" {
  description = "Private subnet IDs for EKS nodes and RDS"
  value       = module.network.private_subnet_ids
}

output "public_subnet_ids" {
  description = "Public subnet IDs for load balancers"
  value       = module.network.public_subnet_ids
}

output "eks_cluster_name" {
  description = "EKS cluster name"
  value       = module.eks.cluster_name
}

output "eks_cluster_endpoint" {
  description = "EKS API server endpoint"
  value       = module.eks.cluster_endpoint
}

output "eks_cluster_oidc_issuer_url" {
  description = "EKS OIDC issuer URL"
  value       = module.eks.cluster_oidc_issuer_url
}

output "eks_oidc_provider_arn" {
  description = "EKS OIDC provider ARN for IRSA"
  value       = module.eks.oidc_provider_arn
}

output "ecr_backend_repository_url" {
  description = "ECR repository URL for backend image"
  value       = module.ecr.backend_repository_url
}

output "ecr_frontend_repository_url" {
  description = "ECR repository URL for frontend image"
  value       = module.ecr.frontend_repository_url
}

output "monitoring_dashboard_name" {
  description = "CloudWatch dashboard name"
  value       = module.monitoring.dashboard_name
}

output "monitoring_alerts_topic_arn" {
  description = "SNS topic ARN for monitoring alerts"
  value       = module.monitoring.alerts_topic_arn
}

output "monthly_budget_name" {
  description = "Monthly AWS budget name"
  value       = module.cost_controls.monthly_budget_name
}

output "cost_anomaly_monitor_arn" {
  description = "Cost Anomaly Detection monitor ARN"
  value       = module.cost_controls.cost_anomaly_monitor_arn
}

output "cost_anomaly_subscription_arn" {
  description = "Cost Anomaly Detection subscription ARN"
  value       = module.cost_controls.cost_anomaly_subscription_arn
}

output "audit_log_bucket_name" {
  description = "S3 bucket used for CloudTrail and AWS Config logs"
  value       = module.security_baseline.audit_log_bucket_name
}

output "cloudtrail_arn" {
  description = "CloudTrail ARN"
  value       = module.security_baseline.cloudtrail_arn
}

output "guardduty_detector_id" {
  description = "GuardDuty detector ID"
  value       = module.security_baseline.guardduty_detector_id
}

output "connection_string" {
  description = "PostgreSQL connection string (replace CHANGE_ME with your db_password)"
  value       = "Host=${module.rds.address};Port=${module.rds.port};Database=${var.db_name};Username=${var.db_username};Password=CHANGE_ME"
  sensitive   = false
}

output "next_steps" {
  description = "What to do after terraform apply"
  value       = var.dns_delegation_mode == "subdomain" ? <<-EOT

    0. Delegate ${module.edge.app_hostname} to Route 53 (keep ${var.domain_name} on Cloudflare):
       - In Cloudflare DNS, delete any existing A/CNAME records for "${var.app_subdomain}".
       - Add four separate NS records:
         Name: ${var.app_subdomain}
         Type: NS
         Content: (each value from terraform output route53_name_servers, DNS only / not proxied)
       - Wait until: dig NS ${module.edge.app_hostname} +short
         shows the awsdns nameservers from route53_name_servers.

    1. Configure kubeconfig:
       aws eks update-kubeconfig --region ${var.aws_region} --name ${module.eks.cluster_name}

    2. Build and push images:
       IMAGE_TAG=$(git rev-parse HEAD)
       docker build -t ${module.ecr.backend_repository_url}:$IMAGE_TAG ./backend
       docker build -t ${module.ecr.frontend_repository_url}:$IMAGE_TAG ./frontend
       aws ecr get-login-password --region ${var.aws_region} | docker login --username AWS --password-stdin ${module.ecr.backend_repository_url}
       docker push ${module.ecr.backend_repository_url}:$IMAGE_TAG
       docker push ${module.ecr.frontend_repository_url}:$IMAGE_TAG

    3. Update k8s manifests:
       - set backend image to ${module.ecr.backend_repository_url}:$IMAGE_TAG
       - set frontend image to ${module.ecr.frontend_repository_url}:$IMAGE_TAG
       - set S3 bucket to ${module.s3.bucket_name}
       - annotate backend service account with ${module.backend_irsa.role_arn}
       - set ExternalSecret remote key to ${module.secrets.application_secret_name}
       - set APP_HOSTNAME / ORIGIN_HOSTNAME to ${module.edge.app_hostname}

  EOT : <<-EOT

    1. Configure kubeconfig:
       aws eks update-kubeconfig --region ${var.aws_region} --name ${module.eks.cluster_name}

    2. Build and push images:
       IMAGE_TAG=$(git rev-parse HEAD)
       docker build -t ${module.ecr.backend_repository_url}:$IMAGE_TAG ./backend
       docker build -t ${module.ecr.frontend_repository_url}:$IMAGE_TAG ./frontend
       aws ecr get-login-password --region ${var.aws_region} | docker login --username AWS --password-stdin ${module.ecr.backend_repository_url}
       docker push ${module.ecr.backend_repository_url}:$IMAGE_TAG
       docker push ${module.ecr.frontend_repository_url}:$IMAGE_TAG

    3. Update k8s manifests:
       - set backend image to ${module.ecr.backend_repository_url}:$IMAGE_TAG
       - set frontend image to ${module.ecr.frontend_repository_url}:$IMAGE_TAG
       - set S3 bucket to ${module.s3.bucket_name}
       - annotate backend service account with ${module.backend_irsa.role_arn}
       - set ExternalSecret remote key to ${module.secrets.application_secret_name}

  EOT
}

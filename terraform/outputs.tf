output "ec2_instance_id" {
  description = "EC2 instance ID"
  value       = var.provision_ec2 ? module.ec2[0].instance_id : null
}

output "ec2_public_ip" {
  description = "Public IP address of the EC2 instance"
  value       = var.provision_ec2 ? module.ec2[0].public_ip : null
}

output "ec2_public_dns" {
  description = "Public DNS name of the EC2 instance"
  value       = var.provision_ec2 ? module.ec2[0].public_dns : null
}

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

output "ec2_iam_role_arn" {
  description = "ARN of the IAM role attached to the EC2 instance"
  value       = var.provision_ec2 ? module.iam[0].role_arn : null
}

output "cloudwatch_log_group" {
  description = "CloudWatch log group name"
  value       = module.logging.log_group_name
}

output "ssh_command" {
  description = "SSH command to connect to the EC2 instance"
  value       = var.provision_ec2 ? "ssh -i ~/.ssh/${var.key_pair_name}.pem ubuntu@${module.ec2[0].public_ip}" : null
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

output "ecr_backend_repository_url" {
  description = "ECR repository URL for backend image"
  value       = module.ecr.backend_repository_url
}

output "ecr_frontend_repository_url" {
  description = "ECR repository URL for frontend image"
  value       = module.ecr.frontend_repository_url
}

output "monitoring_dashboard_name" {
  description = "CloudWatch dashboard name for infrastructure and Joby signals"
  value       = module.monitoring.dashboard_name
}

output "monitoring_alerts_topic_arn" {
  description = "SNS topic ARN for monitoring alerts"
  value       = module.monitoring.alerts_topic_arn
}

output "connection_string" {
  description = "PostgreSQL connection string (replace CHANGE_ME with your db_password)"
  value       = "Host=${module.rds.address};Port=${module.rds.port};Database=${var.db_name};Username=${var.db_username};Password=CHANGE_ME"
  sensitive   = false
}

output "next_steps" {
  description = "What to do after terraform apply"
  value       = <<-EOT

    1. Configure kubeconfig:
       aws eks update-kubeconfig --region ${var.aws_region} --name ${module.eks.cluster_name}

    2. Build and push images:
       docker build -t ${module.ecr.backend_repository_url}:latest ./backend
       docker build -t ${module.ecr.frontend_repository_url}:latest ./frontend
       aws ecr get-login-password --region ${var.aws_region} | docker login --username AWS --password-stdin ${module.ecr.backend_repository_url}
       docker push ${module.ecr.backend_repository_url}:latest
       docker push ${module.ecr.frontend_repository_url}:latest

    3. Update k8s manifests:
       - set backend image to ${module.ecr.backend_repository_url}:latest
       - set frontend image to ${module.ecr.frontend_repository_url}:latest
       - set DB host in k8s secret to ${module.rds.address}
       - set S3 bucket to ${module.s3.bucket_name}

  EOT
}

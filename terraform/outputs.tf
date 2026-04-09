# -----------------------------------------------------------------------------
# EC2
# -----------------------------------------------------------------------------

output "ec2_instance_id" {
  description = "EC2 instance ID"
  value       = module.ec2.instance_id
}

output "ec2_public_ip" {
  description = "Public IP address of the EC2 instance"
  value       = module.ec2.public_ip
}

output "ec2_public_dns" {
  description = "Public DNS name of the EC2 instance"
  value       = module.ec2.public_dns
}

# -----------------------------------------------------------------------------
# RDS
# -----------------------------------------------------------------------------

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

# -----------------------------------------------------------------------------
# S3
# -----------------------------------------------------------------------------

output "s3_bucket_name" {
  description = "S3 bucket name for file uploads"
  value       = module.s3.bucket_name
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN"
  value       = module.s3.bucket_arn
}

# -----------------------------------------------------------------------------
# IAM
# -----------------------------------------------------------------------------

output "ec2_iam_role_arn" {
  description = "ARN of the IAM role attached to the EC2 instance"
  value       = module.iam.role_arn
}

# -----------------------------------------------------------------------------
# CloudWatch
# -----------------------------------------------------------------------------

output "cloudwatch_log_group" {
  description = "CloudWatch log group name"
  value       = aws_cloudwatch_log_group.app.name
}

# -----------------------------------------------------------------------------
# Connectivity helpers
# -----------------------------------------------------------------------------

output "ssh_command" {
  description = "SSH command to connect to the EC2 instance"
  value       = "ssh -i ~/.ssh/${var.key_pair_name}.pem ubuntu@${module.ec2.public_ip}"
}

output "connection_string" {
  description = "PostgreSQL connection string (replace CHANGE_ME with your db_password)"
  value       = "Host=${module.rds.address};Port=${module.rds.port};Database=${var.db_name};Username=${var.db_username};Password=CHANGE_ME"
  sensitive   = false
}

output "next_steps" {
  description = "What to do after terraform apply"
  value       = <<-EOT

    ============================================================
    Joby Infrastructure Deployed Successfully!
    ============================================================

    1. SSH into your EC2 instance:
       ssh -i ~/.ssh/${var.key_pair_name}.pem ubuntu@${module.ec2.public_ip}

    2. Verify Docker is running:
       docker --version && docker compose version

    3. Test RDS connectivity from EC2:
       sudo apt-get install -y postgresql-client
       psql -h ${module.rds.address} -p ${module.rds.port} -U ${var.db_username} -d ${var.db_name}

    4. Test S3 access from EC2 (uses the instance role, no keys needed):
       aws s3 ls s3://${module.s3.bucket_name}
       echo "hello" > /tmp/test.txt
       aws s3 cp /tmp/test.txt s3://${module.s3.bucket_name}/test.txt
       aws s3 rm s3://${module.s3.bucket_name}/test.txt

    5. Edit /opt/${var.project_name}/.env with your real database password

    6. Copy your docker-compose.yml and app configs to /opt/${var.project_name}/

    7. Start the app:
       cd /opt/${var.project_name} && docker compose up -d

  EOT
}

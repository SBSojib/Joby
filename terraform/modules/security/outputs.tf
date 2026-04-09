output "ec2_security_group_id" {
  description = "Security group ID for the EC2 instance"
  value       = aws_security_group.ec2.id
}

output "rds_security_group_id" {
  description = "Security group ID for the RDS instance"
  value       = aws_security_group.rds.id
}

output "instance_profile_name" {
  description = "Name of the IAM instance profile for EC2"
  value       = aws_iam_instance_profile.ec2.name
}

output "role_arn" {
  description = "ARN of the IAM role attached to the EC2 instance"
  value       = aws_iam_role.ec2.arn
}

output "role_name" {
  description = "Name of the IAM role"
  value       = aws_iam_role.ec2.name
}

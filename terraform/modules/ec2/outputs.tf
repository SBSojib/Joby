output "instance_id" {
  description = "EC2 instance ID"
  value       = aws_instance.this.id
}

output "public_ip" {
  description = "Public IP (Elastic IP if enabled, otherwise instance public IP)"
  value       = var.enable_elastic_ip ? aws_eip.this[0].public_ip : aws_instance.this.public_ip
}

output "public_dns" {
  description = "Public DNS name of the EC2 instance"
  value       = var.enable_elastic_ip ? aws_eip.this[0].public_dns : aws_instance.this.public_dns
}

output "private_ip" {
  description = "Private IP of the EC2 instance"
  value       = aws_instance.this.private_ip
}

output "ami_id" {
  description = "AMI ID used for the instance"
  value       = data.aws_ami.ubuntu.id
}

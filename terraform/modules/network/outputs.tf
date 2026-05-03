output "vpc_id" {
  description = "Dedicated VPC ID"
  value       = aws_vpc.this.id
}

output "subnet_ids" {
  description = "Private subnet IDs used by compute and data resources"
  value       = aws_subnet.private[*].id
}

output "public_subnet_ids" {
  description = "Public subnet IDs used for load balancers and optional EC2 access"
  value       = aws_subnet.public[*].id
}

output "private_subnet_ids" {
  description = "Private subnet IDs used by EKS nodes and RDS"
  value       = aws_subnet.private[*].id
}

output "private_route_table_ids" {
  description = "Private route table IDs"
  value       = aws_route_table.private[*].id
}

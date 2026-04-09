output "endpoint" {
  description = "RDS connection endpoint (host:port)"
  value       = aws_db_instance.this.endpoint
}

output "address" {
  description = "RDS hostname (without port)"
  value       = aws_db_instance.this.address
}

output "port" {
  description = "RDS port number"
  value       = aws_db_instance.this.port
}

output "db_name" {
  description = "Name of the PostgreSQL database"
  value       = aws_db_instance.this.db_name
}

output "identifier" {
  description = "RDS instance identifier"
  value       = aws_db_instance.this.identifier
}

output "hosted_zone_id" {
  description = "Route 53 hosted zone ID"
  value       = aws_route53_zone.this.zone_id
}

output "hosted_zone_arn" {
  description = "Route 53 hosted zone ARN"
  value       = aws_route53_zone.this.arn
}

output "hosted_zone_name" {
  description = "Route 53 hosted zone DNS name"
  value       = aws_route53_zone.this.name
}

output "hosted_zone_name_servers" {
  description = "Name servers for the Route 53 hosted zone (delegate these in Cloudflare for subdomain mode)"
  value       = aws_route53_zone.this.name_servers
}

output "app_hostname" {
  description = "Public application hostname"
  value       = local.app_hostname
}

output "certificate_arn" {
  description = "Validated ACM certificate ARN for the ALB listener"
  value       = aws_acm_certificate_validation.app.certificate_arn
}

output "waf_web_acl_arn" {
  description = "Regional WAF Web ACL ARN"
  value       = aws_wafv2_web_acl.app.arn
}

output "waf_web_acl_name" {
  description = "Regional WAF Web ACL name"
  value       = aws_wafv2_web_acl.app.name
}

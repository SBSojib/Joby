output "audit_log_bucket_name" {
  description = "S3 bucket used for CloudTrail and AWS Config logs"
  value       = aws_s3_bucket.audit_logs.id
}

output "cloudtrail_arn" {
  description = "CloudTrail ARN"
  value       = aws_cloudtrail.this.arn
}

output "guardduty_detector_id" {
  description = "GuardDuty detector ID"
  value       = aws_guardduty_detector.this.id
}

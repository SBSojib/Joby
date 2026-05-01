output "alerts_topic_arn" {
  value = var.enable_alerting ? aws_sns_topic.alerts[0].arn : null
}

output "dashboard_name" {
  value = aws_cloudwatch_dashboard.operations.dashboard_name
}

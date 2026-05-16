output "monthly_budget_name" {
  description = "Monthly AWS budget name"
  value       = local.enable_emails ? aws_budgets_budget.monthly[0].name : null
}

output "cost_anomaly_monitor_arn" {
  description = "Cost Anomaly Detection monitor ARN"
  value       = local.enable_emails ? aws_ce_anomaly_monitor.service[0].arn : null
}

output "cost_anomaly_subscription_arn" {
  description = "Cost Anomaly Detection subscription ARN"
  value       = local.enable_emails ? aws_ce_anomaly_subscription.email[0].arn : null
}

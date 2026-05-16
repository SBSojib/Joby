locals {
  name_prefix   = "${var.project_name}-${var.environment}"
  enable_emails = length(trimspace(var.budget_alert_email)) > 0
}

resource "aws_budgets_budget" "monthly" {
  count = local.enable_emails ? 1 : 0

  name         = "${local.name_prefix}-monthly-cost"
  budget_type  = "COST"
  limit_amount = tostring(var.monthly_budget_limit_usd)
  limit_unit   = "USD"
  time_unit    = "MONTHLY"

  cost_filter {
    name = "TagKeyValue"
    values = [
      "user:Project$${var.project_name}",
      "user:Environment$${var.environment}"
    ]
  }

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 80
    threshold_type             = "PERCENTAGE"
    notification_type          = "FORECASTED"
    subscriber_email_addresses = [var.budget_alert_email]
  }

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 100
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_email_addresses = [var.budget_alert_email]
  }
}

resource "aws_ce_anomaly_monitor" "service" {
  count = local.enable_emails ? 1 : 0

  name              = "${local.name_prefix}-services"
  monitor_type      = "DIMENSIONAL"
  monitor_dimension = "SERVICE"
}

resource "aws_ce_anomaly_subscription" "email" {
  count = local.enable_emails ? 1 : 0

  name      = "${local.name_prefix}-cost-anomalies"
  frequency = "DAILY"

  monitor_arn_list = [
    aws_ce_anomaly_monitor.service[0].arn
  ]

  subscriber {
    type    = "EMAIL"
    address = var.budget_alert_email
  }

  threshold_expression {
    dimension {
      key           = "ANOMALY_TOTAL_IMPACT_ABSOLUTE"
      values        = [tostring(var.cost_anomaly_threshold_usd)]
      match_options = ["GREATER_THAN_OR_EQUAL"]
    }
  }

  tags = var.tags
}

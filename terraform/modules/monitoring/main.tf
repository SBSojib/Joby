locals {
  name_prefix        = "${var.project_name}-${var.environment}"
  alarm_actions      = var.enable_alerting ? [aws_sns_topic.alerts[0].arn] : []
  ok_actions         = var.enable_alerting ? [aws_sns_topic.alerts[0].arn] : []
  custom_namespace   = "${var.project_name}/${var.environment}"
  dashboard_name     = "${local.name_prefix}-operations"
  create_subscription = var.enable_alerting && length(trimspace(var.alert_email)) > 0
}

resource "aws_sns_topic" "alerts" {
  count = var.enable_alerting ? 1 : 0
  name  = "${local.name_prefix}-alerts"
  tags  = var.tags
}

resource "aws_sns_topic_subscription" "email" {
  count     = local.create_subscription ? 1 : 0
  topic_arn = aws_sns_topic.alerts[0].arn
  protocol  = "email"
  endpoint  = var.alert_email
}

resource "aws_cloudwatch_log_metric_filter" "app_errors" {
  name           = "${local.name_prefix}-app-errors"
  pattern        = "ERROR"
  log_group_name = var.application_log_group_name

  metric_transformation {
    name      = "AppErrorCount"
    namespace = local.custom_namespace
    value     = "1"
  }
}

resource "aws_cloudwatch_metric_alarm" "rds_cpu_high" {
  alarm_name          = "${local.name_prefix}-rds-cpu-high"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/RDS"
  period              = 300
  statistic           = "Average"
  threshold           = 80
  alarm_description   = "RDS CPU is above 80%."
  alarm_actions       = local.alarm_actions
  ok_actions          = local.ok_actions

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }
}

resource "aws_cloudwatch_metric_alarm" "rds_free_storage_low" {
  alarm_name          = "${local.name_prefix}-rds-free-storage-low"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = 2
  metric_name         = "FreeStorageSpace"
  namespace           = "AWS/RDS"
  period              = 300
  statistic           = "Average"
  threshold           = 5368709120
  alarm_description   = "RDS free storage is below 5 GiB."
  alarm_actions       = local.alarm_actions
  ok_actions          = local.ok_actions

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }
}

resource "aws_cloudwatch_metric_alarm" "rds_connections_high" {
  alarm_name          = "${local.name_prefix}-rds-connections-high"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "DatabaseConnections"
  namespace           = "AWS/RDS"
  period              = 300
  statistic           = "Average"
  threshold           = 100
  alarm_description   = "RDS connections are above 100."
  alarm_actions       = local.alarm_actions
  ok_actions          = local.ok_actions

  dimensions = {
    DBInstanceIdentifier = var.rds_instance_identifier
  }
}

resource "aws_cloudwatch_metric_alarm" "eks_failed_requests" {
  alarm_name          = "${local.name_prefix}-eks-api-failed-requests"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "cluster_failed_request_count"
  namespace           = "AWS/EKS"
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  treat_missing_data  = "notBreaching"
  alarm_description   = "EKS API server is reporting failed requests."
  alarm_actions       = local.alarm_actions
  ok_actions          = local.ok_actions

  dimensions = {
    ClusterName = var.eks_cluster_name
  }
}

resource "aws_cloudwatch_metric_alarm" "joby_app_errors" {
  alarm_name          = "${local.name_prefix}-joby-app-errors"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = aws_cloudwatch_log_metric_filter.app_errors.metric_transformation[0].name
  namespace           = local.custom_namespace
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  treat_missing_data  = "notBreaching"
  alarm_description   = "Joby application logged errors."
  alarm_actions       = local.alarm_actions
  ok_actions          = local.ok_actions
}

resource "aws_cloudwatch_dashboard" "operations" {
  dashboard_name = local.dashboard_name
  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6
        properties = {
          title   = "RDS CPU Utilization"
          region  = var.aws_region
          view    = "timeSeries"
          stat    = "Average"
          period  = 300
          metrics = [["AWS/RDS", "CPUUtilization", "DBInstanceIdentifier", var.rds_instance_identifier]]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 0
        width  = 12
        height = 6
        properties = {
          title   = "RDS Connections"
          region  = var.aws_region
          view    = "timeSeries"
          stat    = "Average"
          period  = 300
          metrics = [["AWS/RDS", "DatabaseConnections", "DBInstanceIdentifier", var.rds_instance_identifier]]
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 6
        width  = 12
        height = 6
        properties = {
          title   = "RDS Free Storage"
          region  = var.aws_region
          view    = "timeSeries"
          stat    = "Average"
          period  = 300
          metrics = [["AWS/RDS", "FreeStorageSpace", "DBInstanceIdentifier", var.rds_instance_identifier]]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 6
        width  = 12
        height = 6
        properties = {
          title   = "EKS API Failed Requests"
          region  = var.aws_region
          view    = "timeSeries"
          stat    = "Sum"
          period  = 300
          metrics = [["AWS/EKS", "cluster_failed_request_count", "ClusterName", var.eks_cluster_name]]
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 12
        width  = 24
        height = 6
        properties = {
          title   = "Joby App Errors"
          region  = var.aws_region
          view    = "timeSeries"
          stat    = "Sum"
          period  = 300
          metrics = [[local.custom_namespace, "AppErrorCount"]]
        }
      }
    ]
  })
}

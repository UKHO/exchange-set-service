resource "azurerm_monitor_autoscale_setting" "azurerm_app_service_autoscale_setting" {
  name                = "${var.asp_name}-autoscale"
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = var.asp_id
  tags                = var.tags

  # Add a special profile for Friday morning if required.
  dynamic "profile" {
    for_each = var.include_friday_am_scaling[var.env_name] ? [1] : []
    content {
      name = "Friday AM"

      capacity {
        default = 4
        minimum = 4
        maximum = var.auto_scale_max_instances
      }

      recurrence {
        timezone = "GMT Standard Time"
        days     = ["Friday"]
        hours    = [8]
        minutes  = [0]
      }

      rule {
        metric_trigger {
          metric_name              = "ApproximateMessageCount"
          metric_resource_id       = var.commit_queue_metric_resource_uri
          time_grain               = "PT1M"
          statistic                = "Average"
          time_window              = "PT1M"
          time_aggregation         = "Average"
          operator                 = "GreaterThan"
          threshold                = var.auto_scale_out_threshold
          divide_by_instance_count = true
        }

        scale_action {
          direction = "Increase"
          type      = "ChangeCount"
          value     = "1"
          cooldown  = "PT1M"
        }
      }

      rule {
        metric_trigger {
          metric_name              = "ApproximateMessageCount"
          metric_resource_id       = var.commit_queue_metric_resource_uri
          time_grain               = "PT1M"
          statistic                = "Average"
          time_window              = "PT1M"
          time_aggregation         = "Average"
          operator                 = "LessThan"
          threshold                = var.auto_scale_in_threshold
          divide_by_instance_count = true
        }

        scale_action {
          direction = "Decrease"
          type      = "ChangeCount"
          value     = "1"
          cooldown  = "PT1M"
        }
      }
    }
  }

  # The default profile. The name and recurrence rules need to be set differently if we've also included the Friday profile above.
  profile {
    name = var.include_friday_am_scaling[var.env_name] ? "{\"name\":\"defaultProfile\",\"for\":\"Friday AM\"}" : "defaultProfile"

    capacity {
      default = 1
      minimum = 1
      maximum = var.auto_scale_max_instances
    }

    dynamic "recurrence" {
      for_each = var.include_friday_am_scaling[var.env_name] ? [1] : []
      content {
        timezone = "GMT Standard Time"
        days     = ["Friday"]
        hours    = [11]
        minutes  = [0]
      }
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.commit_queue_metric_resource_uri
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "GreaterThan"
        threshold                = var.auto_scale_out_threshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT1M"
      }
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.commit_queue_metric_resource_uri
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "LessThan"
        threshold                = var.auto_scale_in_threshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "1"
        cooldown  = "PT1M"
      }
    }
  }
}

resource "azurerm_monitor_autoscale_setting" "autoscale_setting_sxs" {
  count               = var.asp_control_sxs.enableAutoScale ? var.exchange_set_config.SmallExchangeSetInstance : 0
  name                = "${var.asp["sxs"][count.index].name}-autoscale"
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = var.asp["sxs"][count.index].id
  tags                = var.tags

  profile {
    name = "defaultProfile"

    capacity {
      default = var.asp_control_sxs.zoneRedundant ? 3 : 1
      minimum = var.asp_control_sxs.zoneRedundant ? 3 : 1
      maximum = var.asp_control_sxs.autoScaleMaxInstances
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.queue_resource_uri_sxs[count.index]
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "GreaterThan"
        threshold                = var.asp_control_sxs.autoScaleOutThreshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "${var.asp_control_sxs.autoScaleOutAmount}"
        cooldown  = "PT1M"
      }
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.queue_resource_uri_sxs[count.index]
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "LessThan"
        threshold                = var.asp_control_sxs.autoScaleInThreshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "${var.asp_control_sxs.autoScaleInAmount}"
        cooldown  = "PT1M"
      }
    }
  }
}

resource "azurerm_monitor_autoscale_setting" "autoscale_setting_mxs" {
  count               = var.asp_control_mxs.enableAutoScale ? var.exchange_set_config.SmallExchangeSetInstance : 0
  name                = "${var.asp["mxs"][count.index].name}-autoscale"
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = var.asp["mxs"][count.index].id
  tags                = var.tags

  profile {
    name = "defaultProfile"

    capacity {
      default = var.asp_control_mxs.zoneRedundant ? 3 : 1
      minimum = var.asp_control_mxs.zoneRedundant ? 3 : 1
      maximum = var.asp_control_mxs.autoScaleMaxInstances
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.queue_resource_uri_mxs[count.index]
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "GreaterThan"
        threshold                = var.asp_control_mxs.autoScaleOutThreshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "${var.asp_control_mxs.autoScaleOutAmount}"
        cooldown  = "PT1M"
      }
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.queue_resource_uri_mxs[count.index]
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "LessThan"
        threshold                = var.asp_control_mxs.autoScaleInThreshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "${var.asp_control_mxs.autoScaleInAmount}"
        cooldown  = "PT1M"
      }
    }
  }
}

resource "azurerm_monitor_autoscale_setting" "autoscale_setting_lxs" {
  count               = var.asp_control_lxs.enableAutoScale ? var.exchange_set_config.SmallExchangeSetInstance : 0
  name                = "${var.asp["lxs"][count.index].name}-autoscale"
  resource_group_name = var.resource_group_name
  location            = var.location
  target_resource_id  = var.asp["lxs"][count.index].id
  tags                = var.tags

  profile {
    name = "defaultProfile"

    capacity {
      default = var.asp_control_lxs.zoneRedundant ? 3 : 1
      minimum = var.asp_control_lxs.zoneRedundant ? 3 : 1
      maximum = var.asp_control_lxs.autoScaleMaxInstances
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.queue_resource_uri_lxs[count.index]
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "GreaterThan"
        threshold                = var.asp_control_lxs.autoScaleOutThreshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Increase"
        type      = "ChangeCount"
        value     = "${var.asp_control_lxs.autoScaleOutAmount}"
        cooldown  = "PT1M"
      }
    }

    rule {
      metric_trigger {
        metric_name              = "ApproximateMessageCount"
        metric_resource_id       = var.queue_resource_uri_lxs[count.index]
        time_grain               = "PT1M"
        statistic                = "Average"
        time_window              = "PT1M"
        time_aggregation         = "Average"
        operator                 = "LessThan"
        threshold                = var.asp_control_lxs.autoScaleInThreshold
        divide_by_instance_count = true
      }

      scale_action {
        direction = "Decrease"
        type      = "ChangeCount"
        value     = "${var.asp_control_lxs.autoScaleInAmount}"
        cooldown  = "PT1M"
      }
    }
  }
}

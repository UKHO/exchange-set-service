resource "terraform_data" "replacement" {
  input = var.asp_control_webapp.zoneRedundant
}

resource "azurerm_service_plan" "app_service_plan" {
  name                   = "${var.name}-asp"
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.asp_control_webapp.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.asp_control_webapp.zoneRedundant
}

resource "azurerm_windows_web_app" "webapp_service" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement]
  }

  name                      = var.name
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.app_service_plan.id
  tags                      = var.tags
  virtual_network_subnet_id = var.subnet_id

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.subnet_id
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ips
      content {
          ip_address  = length(split("/",ip_restriction.value)) > 1 ? ip_restriction.value : "${ip_restriction.value}/32"
      }
    }
  }

  app_settings = var.app_settings

  sticky_settings {
    app_setting_names = [ "WEBJOBS_STOPPED" ]
  }

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_windows_web_app_slot" "staging" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement]
  }

  name                      = "staging"
  app_service_id            = azurerm_windows_web_app.webapp_service.id
  tags                      = azurerm_windows_web_app.webapp_service.tags
  virtual_network_subnet_id = var.subnet_id

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.subnet_id
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ips
      content {
          ip_address  = length(split("/",ip_restriction.value)) > 1 ? ip_restriction.value : "${ip_restriction.value}/32"
      }
    }
  }

  app_settings = merge(azurerm_windows_web_app.webapp_service.app_settings, { "WEBJOBS_STOPPED" = "1" })

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = azurerm_windows_web_app.webapp_service.https_only
}

# Small exchange set
resource "terraform_data" "replacement_sxs" {
  input = var.asp_control_sxs.zoneRedundant
}

resource "azurerm_service_plan" "small_exchange_set_app_service_plan" {
  count                  = var.exchange_set_config.SmallExchangeSetInstance
  name                   = var.asp_name_sxs[count.index]
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.asp_control_sxs.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.asp_control_sxs.zoneRedundant
}

resource "azurerm_windows_web_app" "small_exchange_set_webapp" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement_sxs]
  }

  count                     = var.exchange_set_config.SmallExchangeSetInstance
  name                      = var.as_name_sxs[count.index]
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.small_exchange_set_app_service_plan[count.index].id
  tags                      = var.tags
  virtual_network_subnet_id = var.small_exchange_set_subnets[count.index]

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false
    
    ip_restriction {
      virtual_network_subnet_id = var.small_exchange_set_subnets[count.index]
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

resource "azurerm_windows_web_app_slot" "small_exchange_set_staging" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement_sxs]
  }

  count                     = var.exchange_set_config.SmallExchangeSetInstance
  name                      = "staging"
  app_service_id            = azurerm_windows_web_app.small_exchange_set_webapp[count.index].id
  tags                      = azurerm_windows_web_app.small_exchange_set_webapp[count.index].tags
  virtual_network_subnet_id = var.small_exchange_set_subnets[count.index]

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.small_exchange_set_subnets[count.index]
    }
  }

  app_settings = merge(azurerm_windows_web_app.small_exchange_set_webapp[count.index].app_settings, { "WEBJOBS_STOPPED" = "1" })

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = azurerm_windows_web_app.small_exchange_set_webapp[count.index].https_only
}

# Medium exchange set
resource "terraform_data" "replacement_mxs" {
  input = var.asp_control_mxs.zoneRedundant
}

resource "azurerm_service_plan" "medium_exchange_set_app_service_plan" {
  count                  = var.exchange_set_config.MediumExchangeSetInstance
  name                   = var.asp_name_mxs[count.index]
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.asp_control_mxs.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.asp_control_mxs.zoneRedundant
}

resource "azurerm_windows_web_app" "medium_exchange_set_webapp" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement_mxs]
  }

  count                     = var.exchange_set_config.MediumExchangeSetInstance
  name                      = var.as_name_mxs[count.index]
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.medium_exchange_set_app_service_plan[count.index].id
  tags                      = var.tags
  virtual_network_subnet_id = var.medium_exchange_set_subnets[count.index]

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false
    
    ip_restriction {
      virtual_network_subnet_id = var.medium_exchange_set_subnets[count.index]
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

resource "azurerm_windows_web_app_slot" "medium_exchange_set_staging" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement_mxs]
  }

  count                     = var.exchange_set_config.MediumExchangeSetInstance
  name                      = "staging"
  app_service_id            = azurerm_windows_web_app.medium_exchange_set_webapp[count.index].id
  tags                      = azurerm_windows_web_app.medium_exchange_set_webapp[count.index].tags
  virtual_network_subnet_id = var.medium_exchange_set_subnets[count.index]

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.medium_exchange_set_subnets[count.index]
    }
  }

  app_settings = merge(azurerm_windows_web_app.medium_exchange_set_webapp[count.index].app_settings, { "WEBJOBS_STOPPED" = "1" })

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = azurerm_windows_web_app.medium_exchange_set_webapp[count.index].https_only
}

# Large exchange set
resource "terraform_data" "replacement_lxs" {
  input = var.asp_control_lxs.zoneRedundant
}

resource "azurerm_service_plan" "large_exchange_set_app_service_plan" {
  count                  = var.exchange_set_config.LargeExchangeSetInstance
  name                   = var.asp_name_lxs[count.index]
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.asp_control_lxs.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.asp_control_lxs.zoneRedundant
}

resource "azurerm_windows_web_app" "large_exchange_set_webapp" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement_lxs]
  }

  count                     = var.exchange_set_config.LargeExchangeSetInstance
  name                      = var.as_name_lxs[count.index]
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.large_exchange_set_app_service_plan[count.index].id
  tags                      = var.tags
  virtual_network_subnet_id = var.large_exchange_set_subnets[count.index]

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false
    
    ip_restriction {
      virtual_network_subnet_id = var.large_exchange_set_subnets[count.index]
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

resource "azurerm_windows_web_app_slot" "large_exchange_set_staging" {
  lifecycle {
    replace_triggered_by = [terraform_data.replacement_lxs]
  }

  count                     = var.exchange_set_config.LargeExchangeSetInstance
  name                      = "staging"
  app_service_id            = azurerm_windows_web_app.large_exchange_set_webapp[count.index].id
  tags                      = azurerm_windows_web_app.large_exchange_set_webapp[count.index].tags
  virtual_network_subnet_id = var.large_exchange_set_subnets[count.index]

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.large_exchange_set_subnets[count.index]
    }
  }

  app_settings = merge(azurerm_windows_web_app.large_exchange_set_webapp[count.index].app_settings, { "WEBJOBS_STOPPED" = "1" })

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = azurerm_windows_web_app.large_exchange_set_webapp[count.index].https_only
}

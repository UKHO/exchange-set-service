resource "azurerm_service_plan" "small_exchange_set_app_service_plan" {
  count                  = var.exchange_set_config.SmallExchangeSetInstance
  name                   = var.asp_name_sxs[count.index]
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.app_service_control_sxs.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.app_service_control_sxs.zoneRedundant
}

resource "azurerm_app_service" "small_exchange_set_webapp" {
  count               = var.exchange_set_config.SmallExchangeSetInstance
  name                = var.as_name_sxs[count.index]
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_service_plan.small_exchange_set_app_service_plan[count.index].id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
    ip_restriction {
      virtual_network_subnet_id = var.small_exchange_set_subnets[count.index]
    }
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_app_service_slot" "small_exchange_set_staging" {
  count               = var.exchange_set_config.SmallExchangeSetInstance
  name                = "staging"
  app_service_name    = azurerm_app_service.small_exchange_set_webapp[count.index].name
  location            = azurerm_app_service.small_exchange_set_webapp[count.index].location
  resource_group_name = azurerm_app_service.small_exchange_set_webapp[count.index].resource_group_name
  app_service_plan_id = azurerm_service_plan.small_exchange_set_app_service_plan[count.index].id
  tags                = azurerm_app_service.small_exchange_set_webapp[count.index].tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
    ip_restriction {
      virtual_network_subnet_id = var.small_exchange_set_subnets[count.index]
    }
  }

  app_settings = merge(azurerm_app_service.small_exchange_set_webapp[count.index].app_settings, {"WEBJOBS_STOPPED"="1"})

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_app_service_virtual_network_swift_connection" "small_exchange_set_webapp_vnet_integration" {
  count = var.exchange_set_config.SmallExchangeSetInstance
  app_service_id = azurerm_app_service.small_exchange_set_webapp[count.index].id
  subnet_id      = var.small_exchange_set_subnets[count.index]
}

resource "azurerm_app_service_slot_virtual_network_swift_connection" "small_exchange_set_slot_vnet_integration" {
  count = var.exchange_set_config.SmallExchangeSetInstance
  app_service_id = azurerm_app_service.small_exchange_set_webapp[count.index].id
  subnet_id      = var.small_exchange_set_subnets[count.index]
  slot_name      = azurerm_app_service_slot.small_exchange_set_staging[count.index].name
}

#Medium exchange set
resource "azurerm_service_plan" "medium_exchange_set_app_service_plan" {
  count                  = var.exchange_set_config.MediumExchangeSetInstance
  name                   = var.asp_name_mxs[count.index]
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.app_service_control_mxs.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.app_service_control_mxs.zoneRedundant
}

resource "azurerm_app_service" "medium_exchange_set_webapp" {
  count               = var.exchange_set_config.MediumExchangeSetInstance
  name                = var.as_name_mxs[count.index]
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_service_plan.medium_exchange_set_app_service_plan[count.index].id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
    ip_restriction {
      virtual_network_subnet_id = var.medium_exchange_set_subnets[count.index]
    }
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_app_service_slot" "medium_exchange_set_staging" {
  count               = var.exchange_set_config.MediumExchangeSetInstance
  name                = "staging"
  app_service_name    = azurerm_app_service.medium_exchange_set_webapp[count.index].name
  location            = azurerm_app_service.medium_exchange_set_webapp[count.index].location
  resource_group_name = azurerm_app_service.medium_exchange_set_webapp[count.index].resource_group_name
  app_service_plan_id = azurerm_service_plan.medium_exchange_set_app_service_plan[count.index].id
  tags                = azurerm_app_service.medium_exchange_set_webapp[count.index].tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
    ip_restriction {
      virtual_network_subnet_id = var.medium_exchange_set_subnets[count.index]
    }
  }

  app_settings = merge(azurerm_app_service.medium_exchange_set_webapp[count.index].app_settings, {"WEBJOBS_STOPPED"="1"})

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_app_service_virtual_network_swift_connection" "medium_exchange_set_webapp_vnet_integration" {
  count = var.exchange_set_config.MediumExchangeSetInstance
  app_service_id = azurerm_app_service.medium_exchange_set_webapp[count.index].id
  subnet_id      = var.medium_exchange_set_subnets[count.index]
}

resource "azurerm_app_service_slot_virtual_network_swift_connection" "medium_exchange_set_slot_vnet_integration" {
  count = var.exchange_set_config.MediumExchangeSetInstance
  app_service_id = azurerm_app_service.medium_exchange_set_webapp[count.index].id
  subnet_id      = var.medium_exchange_set_subnets[count.index]
  slot_name      = azurerm_app_service_slot.medium_exchange_set_staging[count.index].name
}

#Large exchange set
resource "azurerm_service_plan" "large_exchange_set_app_service_plan" {
  count                  = var.exchange_set_config.LargeExchangeSetInstance
  name                   = var.asp_name_lxs[count.index]
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.app_service_control_lxs.sku
  os_type                = "Windows"
  tags                   = var.tags
  zone_balancing_enabled = var.app_service_control_lxs.zoneRedundant
}

resource "azurerm_app_service" "large_exchange_set_webapp" {
  count               = var.exchange_set_config.LargeExchangeSetInstance
  name                = var.as_name_lxs[count.index]
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_service_plan.large_exchange_set_app_service_plan[count.index].id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
    ip_restriction {
      virtual_network_subnet_id = var.large_exchange_set_subnets[count.index]
    }
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_app_service_slot" "large_exchange_set_staging" {
  count               = var.exchange_set_config.LargeExchangeSetInstance
  name                = "staging"
  app_service_name    = azurerm_app_service.large_exchange_set_webapp[count.index].name
  location            = azurerm_app_service.large_exchange_set_webapp[count.index].location
  resource_group_name = azurerm_app_service.large_exchange_set_webapp[count.index].resource_group_name
  app_service_plan_id = azurerm_service_plan.large_exchange_set_app_service_plan[count.index].id
  tags                = azurerm_app_service.large_exchange_set_webapp[count.index].tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
    ip_restriction {
      virtual_network_subnet_id = var.large_exchange_set_subnets[count.index]
    }
  }

  app_settings = merge(azurerm_app_service.large_exchange_set_webapp[count.index].app_settings, {"WEBJOBS_STOPPED"="1"})

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

  https_only = true
}

resource "azurerm_app_service_virtual_network_swift_connection" "large_exchange_set_webapp_vnet_integration" {
  count = var.exchange_set_config.LargeExchangeSetInstance
  app_service_id = azurerm_app_service.large_exchange_set_webapp[count.index].id
  subnet_id      = var.large_exchange_set_subnets[count.index]
}

resource "azurerm_app_service_slot_virtual_network_swift_connection" "large_exchange_set_slot_vnet_integration" {
  count = var.exchange_set_config.LargeExchangeSetInstance
  app_service_id = azurerm_app_service.large_exchange_set_webapp[count.index].id
  subnet_id      = var.large_exchange_set_subnets[count.index]
  slot_name      = azurerm_app_service_slot.large_exchange_set_staging[count.index].name
}

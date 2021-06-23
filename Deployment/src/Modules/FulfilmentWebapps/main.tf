resource "azurerm_app_service_plan" "small_exchange_set_app_service_plan" {
  count               = var.exchange_set_config.SmallExchangeSetInstance
  name                = "${local.small_exchange_set_name}-${count.index}-asp"
  location            = var.location
  resource_group_name = var.resource_group_name
  
  sku {
	tier = var.app_service_sku.tier
	size = var.app_service_sku.size
  }
  tags                = var.tags
}

resource "azurerm_app_service" "small_exchange_set_webapp" {
  count               = var.exchange_set_config.SmallExchangeSetInstance
  name                = "${local.small_exchange_set_name}-${count.index}-webapp"
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.small_exchange_set_app_service_plan[count.index].id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|3.1"
    
    always_on  = true
    ftps_state = "Disabled"
  }

  app_settings = var.app_settings

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
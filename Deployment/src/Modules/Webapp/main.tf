resource "azurerm_app_service_plan" "app_service_plan" {
  name                = "${var.name}-asp"
  location            = var.location
  resource_group_name = var.resource_group_name
  
  sku {
	tier = "PremiumV2"
	size = "P1v2"
  }
  tags                = var.tags
}

resource "azurerm_app_service" "webapp_service" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.app_service_plan.id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|3.1"
    
    always_on  = true
    ftps_state = "Disabled"
  }

  app_settings = var.app_settings

  identity {
    type = "SystemAssigned"
  }

  https_only = true
}

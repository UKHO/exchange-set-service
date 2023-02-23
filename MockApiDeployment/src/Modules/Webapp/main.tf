resource "random_string" "unique_string" {
  length  = 5
  special = false
  upper   = false
}

resource "azurerm_app_service_plan" "app_service_plan" {
  name                = "${var.service_name}-${var.env_name}-${random_string.unique_string.result}-asp"
  location            = var.location
  resource_group_name = var.resource_group_name
  
  sku {
	tier = var.app_service_sku.tier
	size = var.app_service_sku.size
  }
  tags                = var.tags
}

resource "azurerm_app_service" "fulfillment_webapp" {
  name                = "${var.service_name}-${var.env_name}-fulfillment-${random_string.unique_string.result}-webapp"
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.app_service_plan.id
  tags                = var.tags
  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
  }
  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }
}

resource "azurerm_app_service" "scs_fss_mock_webapp" {
  name                = "${var.service_name}-${var.env_name}-mock-${random_string.unique_string.result}-webapp"
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.app_service_plan.id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

}

resource "azurerm_app_service" "ess_webapp" {
  name                = "${var.service_name}-${var.env_name}-${random_string.unique_string.result}-webapp"
  location            = var.location
  resource_group_name = var.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.app_service_plan.id
  tags                = var.tags

  site_config {
    windows_fx_version  =   "DOTNETCORE|6.0"
    
    always_on  = true
    ftps_state = "Disabled"
  }

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }

}

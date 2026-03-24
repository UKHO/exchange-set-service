resource "azurerm_service_plan" "app_service_plan" {
  name                   = "${var.service_name}-${var.env_name}-${var.unique_string}-asp"
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.app_service_sku.size
  os_type                = "Windows"
  tags                   = var.tags
}

resource "azurerm_windows_web_app" "fulfillment_webapp" {
  name                      = "${var.service_name}-${var.env_name}-fulfillment-${var.unique_string}-webapp"
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.app_service_plan.id
  tags                      = var.tags

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }
}

resource "azurerm_windows_web_app" "scs_fss_mock_webapp" {
  name                      = "${var.service_name}-${var.env_name}-mock-${var.unique_string}-webapp"
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.app_service_plan.id
  tags                      = var.tags

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }
}

resource "azurerm_windows_web_app" "ess_webapp" {
  name                      = "${var.service_name}-${var.env_name}-${var.unique_string}-webapp"
  location                  = var.location
  resource_group_name       = var.resource_group_name
  service_plan_id           = azurerm_service_plan.app_service_plan.id
  tags                      = var.tags

  site_config {
    application_stack {
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    
    always_on         = true
    ftps_state        = "Disabled"
    use_32_bit_worker = false
  }

  app_settings = var.app_settings

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }
}

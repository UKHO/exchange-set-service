resource "azurerm_service_plan" "app_service_plan" {
  name                   = "${var.service_name}-${var.env_name}-${var.unique_string}-asp"
  location               = var.location
  resource_group_name    = var.resource_group_name
  sku_name               = var.app_service_sku.size
  os_type                = "Windows"
  tags                   = var.tags
}

#resource "azurerm_app_service" "fulfillment_webapp" {
#  name                = "${var.service_name}-${var.env_name}-fulfillment-${var.unique_string}-webapp"
#  location            = var.location
#  resource_group_name = var.resource_group_name
#  app_service_plan_id = azurerm_service_plan.app_service_plan.id
#  tags                = var.tags
#  site_config {
#    windows_fx_version  =   "DOTNETCORE|6.0"
#    
#    always_on  = true
#    ftps_state = "Disabled"
#  }
#  app_settings = var.app_settings2
#  identity {
#    type = "UserAssigned"
#    identity_ids = [var.user_assigned_identity]
#  }
#}

removed {
  from = module.webapp_service.azurerm_app_service.fulfillment_webapp

  lifecycle {
    destroy = false
  }
}

import {
  to = module.webapp_service.azurerm_windows_web_app.fulfillment_webapp
  id = "/subscriptions/f34020e8-74d2-4c45-b769-362ffa18a656/resourceGroups/essft-qc-webapp-rg/providers/Microsoft.Web/sites/essft-qc-fulfillment-yh3r1-webapp"
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

  app_settings = var.app_settings_with_insights

  identity {
    type = "UserAssigned"
    identity_ids = [var.user_assigned_identity]
  }
}

#resource "azurerm_app_service" "scs_fss_mock_webapp" {
#  name                = "${var.service_name}-${var.env_name}-mock-${var.unique_string}-webapp"
#  location            = var.location
#  resource_group_name = var.resource_group_name
#  app_service_plan_id = azurerm_service_plan.app_service_plan.id
#  tags                = var.tags
#
#  site_config {
#    windows_fx_version  =   "DOTNETCORE|6.0"
#    
#    always_on  = true
#    ftps_state = "Disabled"
#  }
#
#  app_settings = var.app_settings
#
#  identity {
#    type = "UserAssigned"
#    identity_ids = [var.user_assigned_identity]
#  }
#
#}

removed {
  from = module.webapp_service.azurerm_app_service.scs_fss_mock_webapp

  lifecycle {
    destroy = false
  }
}

import {
  to = module.webapp_service.azurerm_windows_web_app.scs_fss_mock_webapp
  id = "/subscriptions/f34020e8-74d2-4c45-b769-362ffa18a656/resourceGroups/essft-qc-webapp-rg/providers/Microsoft.Web/sites/essft-qc-mock-yh3r1-webapp"
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

#resource "azurerm_app_service" "ess_webapp" {
#  name                = "${var.service_name}-${var.env_name}-${var.unique_string}-webapp"
#  location            = var.location
#  resource_group_name = var.resource_group_name
#  app_service_plan_id = azurerm_service_plan.app_service_plan.id
#  tags                = var.tags
#
#  site_config {
#    windows_fx_version  =   "DOTNETCORE|6.0"
#    
#    always_on  = true
#    ftps_state = "Disabled"
#  }

#  identity {
#    type = "UserAssigned"
#    identity_ids = [var.user_assigned_identity]
#  }
#
#}

removed {
  from = module.webapp_service.azurerm_app_service.ess_webapp

  lifecycle {
    destroy = false
  }
}

import {
  to = module.webapp_service.azurerm_windows_web_app.ess_webapp
  id = "/subscriptions/f34020e8-74d2-4c45-b769-362ffa18a656/resourceGroups/essft-qc-webapp-rg/providers/Microsoft.Web/sites/essft-qc-yh3r1-webapp"
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

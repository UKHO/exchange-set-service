data "azurerm_subnet" "subnet" {
  name                 = var.spoke_subnet_name
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

module "app_insights" {
  source              = "./Modules/AppInsights"
  name                = "${local.service_name}-${local.env_name}-insights"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = local.tags

}

module "eventhub" {
  source              = "./Modules/EventHub"
  name                = "${local.service_name}-${local.env_name}-events"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  logstashStorageName = lower("${local.service_name}logstash${local.env_name}")
  tags                = local.tags
}

module "webapp_service" {
  source              = "./Modules/Webapp"
  name                = local.web_app_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  subnet_id           = data.azurerm_subnet.subnet.id

  app_settings = {
    "EventHubLoggingConfiguration:Environment"             = local.env_name
    "EventHubLoggingConfiguration:MinimumLoggingLevel"     = "Warning"
    "EventHubLoggingConfiguration:UkhoMinimumLoggingLevel" = "Information"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = module.app_insights.instrumentation_key
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
  }
  tags = local.tags
}

module "fulfilment_vnet" {
  source              = "./Modules/FulfilmentVnet"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  exchange_set_config = local.config_data.ESSFulfilmentConfiguration
  env_name            = local.env_name
  service_name        = local.service_name
  tags = local.tags
}

module "fulfilment_webapp" {
  source              = "./Modules/FulfilmentWebapps"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  small_exchange_set_subnets = module.fulfilment_vnet.small_exchange_set_subnets
  exchange_set_config = local.config_data.ESSFulfilmentConfiguration
  env_name            = local.env_name
  service_name        = local.service_name
  app_settings = {
    "EventHubLoggingConfiguration:Environment"             = local.env_name
    "EventHubLoggingConfiguration:MinimumLoggingLevel"     = "Warning"
    "EventHubLoggingConfiguration:UkhoMinimumLoggingLevel" = "Information"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = module.app_insights.instrumentation_key
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
  }
  tags = local.tags
}

module "fulfilment_storage" {
  source              = "./Modules/FulfilmentStorage"
  resource_group_name = azurerm_resource_group.rg.name
  allowed_ips         = var.allowed_ips
  location            = var.location
  tags                = local.tags
  small_exchange_set_subnets           = module.fulfilment_vnet.small_exchange_set_subnets
  exchange_set_config = local.config_data.ESSFulfilmentConfiguration
  env_name            = local.env_name
  service_name        = local.service_name
}

module "key_vault" {
  source              = "./Modules/KeyVault"
  name                = local.key_vault_name
  resource_group_name = azurerm_resource_group.rg.name
  env_name            = local.env_name
  tenant_id           = module.webapp_service.web_app_tenant_id
  location            = azurerm_resource_group.rg.location
  allowed_ips         = var.allowed_ips
  subnet_id           = data.azurerm_subnet.subnet.id
  read_access_objects = {
    "webapp_service" = module.webapp_service.web_app_object_id
  }
  secrets = {
    "EventHubLoggingConfiguration--ConnectionString"       = module.eventhub.log_primary_connection_string
    "EventHubLoggingConfiguration--EntityPath"             = module.eventhub.entity_path
    "ESSFulfilmentConfiguration--StorageAccountName" = module.fulfilment_storage.small_exchange_set_name
    "ESSFulfilmentConfiguration--StorageAccountKey"  = module.fulfilment_storage.small_exchange_set_primary_access_key
    "ESSFulfilmentConfiguration--SmallExchangeSetAccountName" = module.fulfilment_storage.small_exchange_set_name
    "ESSFulfilmentConfiguration--SmallExchangeSetAccountKey" = module.fulfilment_storage.small_exchange_set_primary_access_key
  }
  tags                                         = local.tags
}

module "fulfilment_keyvaults" {
  source              = "./Modules/FulfilmentKeyVault"
  service_name        = local.service_name
  resource_group_name = azurerm_resource_group.rg.name
  env_name            = local.env_name
  tenant_id           = module.webapp_service.web_app_tenant_id
  location            = azurerm_resource_group.rg.location
  allowed_ips         = var.allowed_ips
  small_exchange_set_subnets           = module.fulfilment_vnet.small_exchange_set_subnets
  small_exchange_set_read_access_objects = module.fulfilment_webapp.small_exchange_set_web_app_object_ids
  small_exchange_set_secrets = {
    "EventHubLoggingConfiguration--ConnectionString"            = module.eventhub.log_primary_connection_string
    "EventHubLoggingConfiguration--EntityPath"                  = module.eventhub.entity_path
    "ESSFulfilmentStorageConfiguration--StorageAccountName"     = module.fulfilment_storage.small_exchange_set_name
    "ESSFulfilmentStorageConfiguration--StorageAccountKey"      = module.fulfilment_storage.small_exchange_set_primary_access_key
    "AzureWebJobsStorage"                                       = module.fulfilment_storage.small_exchange_set_connection_string
  }
  tags                                         = local.tags
}
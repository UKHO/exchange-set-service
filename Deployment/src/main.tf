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

module "storage" {
  source              = "./Modules/Storage"
  name                = "${local.service_name}${local.env_name}storageukho"
  resource_group_name = azurerm_resource_group.rg.name
  allowed_ips         = var.allowed_ips
  location            = var.location
  tags                = local.tags
  env_name            = local.env_name
  subnet_id           = data.azurerm_subnet.subnet.id
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
  trusted_ips = split(",", module.webapp_service.possible_outbound_ip_addresses)
  secrets = {
    "EventHubLoggingConfiguration--ConnectionString"       = module.eventhub.log_primary_connection_string
    "EventHubLoggingConfiguration--EntityPath"             = module.eventhub.entity_path
    "essFulfilmentStorageConfiguration--StorageAccountName" = module.storage.name
    "essFulfilmentStorageConfiguration--StorageAccountKey"  = module.storage.primary_access_key
  }
  tags                                         = local.tags
}
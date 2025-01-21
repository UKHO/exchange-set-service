data "azurerm_subnet" "main_subnet" {
  name                 = var.spoke_subnet_name
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

data "azurerm_subnet" "small_exchange_set_subnet" {
  count                = local.config_data.ESSFulfilmentConfiguration.SmallExchangeSetInstance
  name                 = "ess-fulfilment-service-s-${sum([1,count.index])}"
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

data "azurerm_subnet" "medium_exchange_set_subnet" {
  count                = local.config_data.ESSFulfilmentConfiguration.MediumExchangeSetInstance
  name                 = "ess-fulfilment-service-m-${sum([1,count.index])}"
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

data "azurerm_subnet" "large_exchange_set_subnet" {
  count                = local.config_data.ESSFulfilmentConfiguration.LargeExchangeSetInstance
  name                 = "ess-fulfilment-service-l-${sum([1,count.index])}"
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

module "user_identity" {
  source              = "./Modules/UserIdentity"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  suffix              = var.suffix
  env_name            = local.env_name
  tags                = local.tags
}

module "app_insights" {
  source              = "./Modules/AppInsights"
  name                = "${local.service_name}-${local.env_name}-insights${var.suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = local.tags
}

module "eventhub" {
  source              = "./Modules/EventHub"
  name                = "${local.service_name}-${local.env_name}-events${var.suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  logstashStorageName = lower("${local.service_name}logstash${local.env_name}${var.storage_suffix}")
  suffix              = var.suffix
  m_spoke_subnet      = data.azurerm_subnet.main_subnet.id
  allowed_ips         = var.allowed_ips  
  tags                = local.tags
  agent_2204_subnet   = var.agent_2204_subnet
  agent_prd_subnet    = var.agent_prd_subnet
}

module "webapp_service" {
  source                    = "./Modules/Webapp"
  name                      = local.web_app_name
  resource_group_name       = azurerm_resource_group.rg.name
  location                  = azurerm_resource_group.rg.location
  subnet_id                 = data.azurerm_subnet.main_subnet.id
  user_assigned_identity    = module.user_identity.ess_service_identity_id
  app_service_sku           = var.app_service_sku[local.env_name]
  app_settings = {
    "EventHubLoggingConfiguration:Environment"             = local.env_name
    "EventHubLoggingConfiguration:MinimumLoggingLevel"     = "Warning"
    "EventHubLoggingConfiguration:UkhoMinimumLoggingLevel" = "Information"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = module.app_insights.instrumentation_key
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                             = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                      = "true"
    "WEBSITE_ADD_SITENAME_BINDINGS_IN_APPHOST_CONFIG"      = "1"
  }
  tags                      = local.tags
  allowed_ips               = var.allowed_ips
}

module "fulfilment_webapp" {
  source                        = "./Modules/FulfilmentWebapps"
  resource_group_name           = azurerm_resource_group.rg.name
  location                      = azurerm_resource_group.rg.location
  small_exchange_set_subnets    = data.azurerm_subnet.small_exchange_set_subnet[*].id
  medium_exchange_set_subnets   = data.azurerm_subnet.medium_exchange_set_subnet[*].id
  large_exchange_set_subnets    = data.azurerm_subnet.large_exchange_set_subnet[*].id
  suffix                        = var.suffix
  exchange_set_config           = local.config_data.ESSFulfilmentConfiguration
  env_name                      = local.env_name
  service_name                  = local.service_name
  user_assigned_identity        = module.user_identity.ess_service_identity_id
  app_service_sku               = var.app_service_sku[local.env_name]
  app_settings = {
    "EventHubLoggingConfiguration:Environment"             = local.env_name
    "EventHubLoggingConfiguration:MinimumLoggingLevel"     = "Warning"
    "EventHubLoggingConfiguration:UkhoMinimumLoggingLevel" = "Information"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = module.app_insights.instrumentation_key
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                             = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                      = "true"
    "ELASTIC_APM_SERVER_URL"                               = var.elastic_apm_server_url
    "ELASTIC_APM_ENVIRONMENT"                              = local.env_name
    "ELASTIC_APM_SERVICE_NAME"                             = "ESS Fulfilment Service"
    "ELASTIC_APM_API_KEY"                                  = var.elastic_apm_api_key
  }
  tags = local.tags
}

module "fulfilment_storage" {
  source                                = "./Modules/FulfilmentStorage"
  resource_group_name                   = azurerm_resource_group.rg.name
  allowed_ips                           = var.allowed_ips  
  location                              = var.location
  tags                                  = local.tags
  small_exchange_set_subnets            = data.azurerm_subnet.small_exchange_set_subnet[*].id
  medium_exchange_set_subnets           = data.azurerm_subnet.medium_exchange_set_subnet[*].id
  large_exchange_set_subnets            = data.azurerm_subnet.large_exchange_set_subnet[*].id
  suffix                                = var.storage_suffix
  m_spoke_subnet                        = data.azurerm_subnet.main_subnet.id
  exchange_set_config                   = local.config_data.ESSFulfilmentConfiguration
  env_name                              = local.env_name
  service_name                          = local.service_name
  agent_2204_subnet                     = var.agent_2204_subnet
  agent_prd_subnet                      = var.agent_prd_subnet
}

module "key_vault" {
  source              = "./Modules/KeyVault"
  name                = local.key_vault_name
  resource_group_name = azurerm_resource_group.rg.name
  env_name            = local.env_name
  tenant_id           = module.user_identity.ess_service_identity_tenant_id
  location            = azurerm_resource_group.rg.location
  allowed_ips         = var.allowed_ips  
  subnet_id           = data.azurerm_subnet.main_subnet.id
  agent_2204_subnet   = var.agent_2204_subnet
  agent_prd_subnet    = var.agent_prd_subnet
  read_access_objects = {
    "ess_service_identity" = module.user_identity.ess_service_identity_principal_id   
  }
  secrets = merge(
      {
        "EventHubLoggingConfiguration--ConnectionString"            = module.eventhub.log_primary_connection_string
        "EventHubLoggingConfiguration--EntityPath"                  = module.eventhub.entity_path
        "ESSFulfilmentConfiguration--StorageAccountName"            = module.fulfilment_storage.small_exchange_set_name
        "ESSFulfilmentConfiguration--StorageAccountKey"             = module.fulfilment_storage.small_exchange_set_primary_access_key
        "ESSFulfilmentConfiguration--SmallExchangeSetAccountName"   = module.fulfilment_storage.small_exchange_set_name
        "ESSFulfilmentConfiguration--SmallExchangeSetAccountKey"    = module.fulfilment_storage.small_exchange_set_primary_access_key
        "ESSFulfilmentConfiguration--MediumExchangeSetAccountName"  = module.fulfilment_storage.medium_exchange_set_name
        "ESSFulfilmentConfiguration--MediumExchangeSetAccountKey"   = module.fulfilment_storage.medium_exchange_set_primary_access_key
        "ESSFulfilmentConfiguration--LargeExchangeSetAccountName"   = module.fulfilment_storage.large_exchange_set_name
        "ESSFulfilmentConfiguration--LargeExchangeSetAccountKey"    = module.fulfilment_storage.large_exchange_set_primary_access_key
        "CacheConfiguration--CacheStorageAccountName"               = module.cache_storage.cache_storage_name
        "CacheConfiguration--CacheStorageAccountKey"                = module.cache_storage.cache_storage_primary_access_key
      },
      var.storage_suffix == "v2" ? {} : {
        "ESSFulfilmentConfiguration--ExchangeSetStorageAccountName" = module.storage[0].ess_storage_name,
        "ESSFulfilmentConfiguration--ExchangeSetStorageAccountKey"  = module.storage[0].ess_storage_primary_access_key
      },
      module.fulfilment_webapp.small_exchange_set_scm_credentials,
      module.fulfilment_webapp.medium_exchange_set_scm_credentials,
      module.fulfilment_webapp.large_exchange_set_scm_credentials
  )
  tags                                         = local.tags
}

module "fulfilment_keyvaults" {
  source                                    = "./Modules/FulfilmentKeyVault"
  service_name                              = local.service_name
  resource_group_name                       = azurerm_resource_group.rg.name
  env_name                                  = local.env_name
  suffix                                    = var.suffix  
  tenant_id                                 = module.user_identity.ess_service_identity_tenant_id
  location                                  = azurerm_resource_group.rg.location
  allowed_ips                               = var.allowed_ips 
  small_exchange_set_subnets                = data.azurerm_subnet.small_exchange_set_subnet[*].id
  medium_exchange_set_subnets               = data.azurerm_subnet.medium_exchange_set_subnet[*].id
  large_exchange_set_subnets                = data.azurerm_subnet.large_exchange_set_subnet[*].id
  agent_2204_subnet                         = var.agent_2204_subnet
  agent_prd_subnet                          = var.agent_prd_subnet
    read_access_objects = {
        "ess_service_identity" = module.user_identity.ess_service_identity_principal_id
  }
  small_exchange_set_secrets = {
    "EventHubLoggingConfiguration--ConnectionString"            = module.eventhub.log_primary_connection_string
    "EventHubLoggingConfiguration--EntityPath"                  = module.eventhub.entity_path
    "ESSFulfilmentStorageConfiguration--StorageAccountName"     = module.fulfilment_storage.small_exchange_set_name
    "ESSFulfilmentStorageConfiguration--StorageAccountKey"      = module.fulfilment_storage.small_exchange_set_primary_access_key
    "AzureWebJobsStorage"                                       = module.fulfilment_storage.small_exchange_set_connection_string
    "CacheConfiguration--CacheStorageAccountName"               = module.cache_storage.cache_storage_name
    "CacheConfiguration--CacheStorageAccountKey"                = module.cache_storage.cache_storage_primary_access_key

  }
  medium_exchange_set_secrets = {
    "EventHubLoggingConfiguration--ConnectionString"            = module.eventhub.log_primary_connection_string
    "EventHubLoggingConfiguration--EntityPath"                  = module.eventhub.entity_path
    "ESSFulfilmentStorageConfiguration--StorageAccountName"     = module.fulfilment_storage.medium_exchange_set_name
    "ESSFulfilmentStorageConfiguration--StorageAccountKey"      = module.fulfilment_storage.medium_exchange_set_primary_access_key
    "AzureWebJobsStorage"                                       = module.fulfilment_storage.medium_exchange_set_connection_string
    "CacheConfiguration--CacheStorageAccountName"               = module.cache_storage.cache_storage_name
    "CacheConfiguration--CacheStorageAccountKey"                = module.cache_storage.cache_storage_primary_access_key
  }
  large_exchange_set_secrets = {
    "EventHubLoggingConfiguration--ConnectionString"            = module.eventhub.log_primary_connection_string
    "EventHubLoggingConfiguration--EntityPath"                  = module.eventhub.entity_path
    "ESSFulfilmentStorageConfiguration--StorageAccountName"     = module.fulfilment_storage.large_exchange_set_name
    "ESSFulfilmentStorageConfiguration--StorageAccountKey"      = module.fulfilment_storage.large_exchange_set_primary_access_key
    "AzureWebJobsStorage"                                       = module.fulfilment_storage.large_exchange_set_connection_string
    "CacheConfiguration--CacheStorageAccountName"               = module.cache_storage.cache_storage_name
    "CacheConfiguration--CacheStorageAccountKey"                = module.cache_storage.cache_storage_primary_access_key
  }
  tags                                      = local.tags
}

module "azure-dashboard" {
  source         = "./Modules/azuredashboard"
  name           = "ESS-${local.env_name}-Monitoring-Dashboard${var.suffix}"
  location       = azurerm_resource_group.rg.location
  environment    = local.env_name
  resource_group = azurerm_resource_group.rg
  tags           = local.tags
}

module "cache_storage" {
  source                                = "./Modules/CacheStorage"
  name                                  = local.env_name == "prod" && var.storage_suffix == "v2" ? "${local.service_name}${local.env_name}cachestorageukho2" : "${local.service_name}${local.env_name}cachestorageukho${var.storage_suffix}"
  resource_group_name                   = azurerm_resource_group.rg.name
  allowed_ips                           = var.allowed_ips  
  location                              = var.location
  tags                                  = local.tags
  small_exchange_set_subnets            = data.azurerm_subnet.small_exchange_set_subnet[*].id
  medium_exchange_set_subnets           = data.azurerm_subnet.medium_exchange_set_subnet[*].id
  large_exchange_set_subnets            = data.azurerm_subnet.large_exchange_set_subnet[*].id
  m_spoke_subnet                        = data.azurerm_subnet.main_subnet.id
  service_name                          = local.service_name
  agent_2204_subnet                     = var.agent_2204_subnet
  agent_prd_subnet                      = var.agent_prd_subnet
}

module "storage" {
  source                                = "./Modules/Storage"
  count                                 = var.storage_suffix == "v2" ? 0 : 1
  name                                  = "${local.service_name}${local.env_name}corestorage"
  resource_group_name                   = azurerm_resource_group.rg.name
  allowed_ips                           = var.allowed_ips  
  location                              = var.location
  tags                                  = local.tags
  small_exchange_set_subnets            = data.azurerm_subnet.small_exchange_set_subnet[*].id
  medium_exchange_set_subnets           = data.azurerm_subnet.medium_exchange_set_subnet[*].id
  large_exchange_set_subnets            = data.azurerm_subnet.large_exchange_set_subnet[*].id
  m_spoke_subnet                        = data.azurerm_subnet.main_subnet.id
  service_name                          = local.service_name
  agent_2204_subnet                     = var.agent_2204_subnet
  agent_prd_subnet                      = var.agent_prd_subnet  
}

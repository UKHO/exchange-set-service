module "user_identity" {
  source              = "./Modules/UserIdentity"
  name                = local.managed_identity_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  env_name            = local.env_name
  service_name        = local.service_name
  tags                = local.tags
}

module "webapp_service" {
  source              = "./Modules/Webapp"
  service_name        = local.service_name
  env_name            = local.env_name
  resource_group_name = azurerm_resource_group.webapp_rg.name
  location            = azurerm_resource_group.webapp_rg.location
  app_service_sku     = var.app_service_sku[local.env_name]
  user_assigned_identity    = module.user_identity.service_identity_id
  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                             = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                      = "true"
  }
  tags = local.tags

}

module "storage" {
  source              = "./Modules/Storage"
  name                = local.storage_account_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = local.tags
}

module "key_vault" {
  source              = "./Modules/KeyVault"
  name                = local.key_vault_name
  resource_group_name = azurerm_resource_group.rg.name
  env_name            = local.env_name
  tenant_id           = module.user_identity.service_identity_tenant_id
  location            = azurerm_resource_group.rg.location
  read_access_objects = {
    "service_identity" = module.user_identity.service_identity_principal_id
  }
  secrets = {
    "AzureStorageConfiguration--ConnectionString"          = module.storage.storage_connection_string
  }
  tags                                         = local.tags
}
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

  app_settings = {
    "EventHubLoggingConfiguration:Environment"             = local.env_name
    "EventHubLoggingConfiguration:MinimumLoggingLevel"     = "Warning"
    "EventHubLoggingConfiguration:UkhoMinimumLoggingLevel" = "Information"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = module.app_insights.instrumentation_key
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
  }
  tags = local.tags
}

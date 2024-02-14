resource "azurerm_log_analytics_workspace" "logs_analytics_workspace" {
  name                = "${var.name}-workspace"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_application_insights" "app_insights" {
  name                 = var.name
  location             = var.location
  resource_group_name  = var.resource_group_name
  application_type     = "web"
  daily_data_cap_in_gb = 5
  workspace_id         = azurerm_log_analytics_workspace.logs_analytics_workspace.id
  tags                 = var.tags
}

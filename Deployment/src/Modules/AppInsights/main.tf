resource "azurerm_application_insights" "app_insights" {
  name                 = var.name
  location             = var.location
  resource_group_name  = var.resource_group_name
  application_type     = "web"
  daily_data_cap_in_gb = 5
  tags                 = var.tags
}

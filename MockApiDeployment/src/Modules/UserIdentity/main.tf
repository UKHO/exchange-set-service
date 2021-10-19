resource "azurerm_user_assigned_identity" "service_identity" {
  resource_group_name = var.resource_group_name
  location            = var.location
  name				  = var.name
  tags				  = var.tags
}
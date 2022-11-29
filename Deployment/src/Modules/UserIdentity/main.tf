resource "azurerm_user_assigned_identity" "ess_service_identity" {
  resource_group_name = var.resource_group_name
  location            = var.location
  name				  = "ess-${var.env_name}-service-identity"
  tags				  = var.tags
}
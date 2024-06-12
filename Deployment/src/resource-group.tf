resource "azurerm_resource_group" "rg" {
  name     = "${var.resource_group_name}-${local.env_name}-rg${var.suffix}"
  location = var.location
  tags     = local.tags
}
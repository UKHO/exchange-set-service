resource "azurerm_resource_group" "webapp_rg" {
  name     = "${local.service_name}-${local.env_name}-webapp-rg"
  location = var.location
  tags     = local.tags
}

resource "azurerm_resource_group" "rg" {
  name     = "${local.service_name}-${local.env_name}-rg"
  location = var.location
  tags     = local.tags
}

resource "azurerm_virtual_network" "vnet" {
  name = lower("${var.service_name}-${var.env_name}-fulfilment-vnet")
  address_space = ["10.120.0.0/16"]
  location = var.location
  resource_group_name = var.resource_group_name
  tags = var.tags
}

resource "azurerm_subnet" "small_exchange_set_subnet" {
  count = var.exchange_set_config.SmallExchangeSetInstance
  name = "${var.service_name}-fulfilment-sxs-${count.index}-subnet"
  resource_group_name = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes = ["10.120.${sum([1,count.index])}.0/24"]

  delegation {
    name = "delegation"

    service_delegation {
      name = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
  service_endpoints = ["Microsoft.Web","Microsoft.KeyVault","Microsoft.Storage"]
}
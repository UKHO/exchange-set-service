resource "azurerm_storage_account" "small_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}sxsstorageukho")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"

  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.small_exchange_set_subnets,[var.m_spoke_subnet,var.agent_subnet])
  }

  tags = var.tags
}

resource "azurerm_storage_container" "small_exchange_set_storage_container" {
  name                  = "ess-fulfilment"
  storage_account_name  = azurerm_storage_account.small_exchange_set_storage.name
}

resource "azurerm_storage_queue" "small_exchange_set_storage_queue" {
  count                = var.exchange_set_config.SmallExchangeSetInstance
  name                 = "ess-${sum([1,count.index])}-fulfilment"
  storage_account_name = azurerm_storage_account.small_exchange_set_storage.name
}

#Medium exchange set storage

resource "azurerm_storage_account" "medium_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}mxsstorageukho")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"

  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.medium_exchange_set_subnets,[var.m_spoke_subnet,var.agent_subnet])
  }

  tags = var.tags
}

resource "azurerm_storage_container" "medium_exchange_set_storage_container" {
  name                  = "ess-fulfilment"
  storage_account_name  = azurerm_storage_account.medium_exchange_set_storage.name
}

resource "azurerm_storage_queue" "medium_exchange_set_storage_queue" {
  count                = var.exchange_set_config.MediumExchangeSetInstance
  name                 = "ess-${sum([1,count.index])}-fulfilment"
  storage_account_name = azurerm_storage_account.medium_exchange_set_storage.name
}

#Large exchange set storage

resource "azurerm_storage_account" "large_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}lxsstorageukho")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"

  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.large_exchange_set_subnets,[var.m_spoke_subnet,var.agent_subnet])
  }

  tags = var.tags
}

resource "azurerm_storage_container" "large_exchange_set_storage_container" {
  name                  = "ess-fulfilment"
  storage_account_name  = azurerm_storage_account.large_exchange_set_storage.name
}

resource "azurerm_storage_queue" "large_exchange_set_storage_queue" {
  count                = var.exchange_set_config.LargeExchangeSetInstance
  name                 = "ess-${sum([1,count.index])}-fulfilment"
  storage_account_name = azurerm_storage_account.large_exchange_set_storage.name
}
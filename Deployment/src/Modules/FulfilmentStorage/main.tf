resource "azurerm_storage_account" "small_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}sxsstorageukho${var.suffix}")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"
  min_tls_version = "TLS1_2"
  allow_nested_items_to_be_public  = false
  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.small_exchange_set_subnets,[var.m_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet])
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

resource "azurerm_storage_management_policy" "small_exchange_set_storage_policy" {
  storage_account_id = azurerm_storage_account.small_exchange_set_storage.id

  rule {
    name    = azurerm_storage_container.small_exchange_set_storage_container.name + "-rule"
    enabled = true

    filters {
      prefix_match = [azurerm_storage_container.small_exchange_set_storage_container.name]
      blob_types   = ["blockBlob", "appendBlob"]
    }

    actions {
      base_blob {
        delete_after_days_since_creation_greater_than = 7
      }
    }
  }
}

#Medium exchange set storage

resource "azurerm_storage_account" "medium_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}mxsstorageukho${var.suffix}")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"
  min_tls_version = "TLS1_2"
  allow_nested_items_to_be_public     = false
  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.medium_exchange_set_subnets,[var.m_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet])
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

resource "azurerm_storage_management_policy" "medium_exchange_set_storage_policy" {
  storage_account_id = azurerm_storage_account.medium_exchange_set_storage.id

  rule {
    name    = azurerm_storage_container.medium_exchange_set_storage_container.name + "-rule"
    enabled = true

    filters {
      prefix_match = [azurerm_storage_container.medium_exchange_set_storage_container.name]
      blob_types   = ["blockBlob", "appendBlob"]
    }

    actions {
      base_blob {
        delete_after_days_since_creation_greater_than = 7
      }
    }
  }
}

#Large exchange set storage

resource "azurerm_storage_account" "large_exchange_set_storage" {
  name = lower("${var.service_name}${var.env_name}lxsstorageukho${var.suffix}")
  resource_group_name = var.resource_group_name
  location = var.location
  account_tier = "Standard"
  account_replication_type = "LRS"
  account_kind = "StorageV2"
  min_tls_version = "TLS1_2"
  allow_nested_items_to_be_public     = false
  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = concat(var.large_exchange_set_subnets,[var.m_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet])
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

resource "azurerm_storage_management_policy" "large_exchange_set_storage_policy" {
  storage_account_id = azurerm_storage_account.large_exchange_set_storage.id

  rule {
    name    = azurerm_storage_container.large_exchange_set_storage_container.name + "-rule"
    enabled = true

    filters {
      prefix_match = [azurerm_storage_container.large_exchange_set_storage_container.name]
      blob_types   = ["blockBlob", "appendBlob"]
    }

    actions {
      base_blob {
        delete_after_days_since_creation_greater_than = 7
      }
    }
  }
}

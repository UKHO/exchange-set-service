resource "azurerm_eventhub_namespace" "eventhub_namespace" {
  name                = "${var.name}-namespace"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
  capacity            = 1
  tags                = var.tags
  minimum_tls_version = "1.2"
}

resource "azurerm_eventhub" "eventhub" {
  name                = var.name
  namespace_name      = azurerm_eventhub_namespace.eventhub_namespace.name
  resource_group_name = var.resource_group_name
  partition_count     = 2
  message_retention   = 7
}

resource "azurerm_eventhub_consumer_group" "logstash_consumer_group" {
  name                = "logstash${var.suffix}"
  namespace_name      = azurerm_eventhub_namespace.eventhub_namespace.name
  eventhub_name       = azurerm_eventhub.eventhub.name
  resource_group_name = var.resource_group_name
}

resource "azurerm_eventhub_consumer_group" "logging_application_consumer_group" {
  name                = "loggingApplication${var.suffix}"
  namespace_name      = azurerm_eventhub_namespace.eventhub_namespace.name
  eventhub_name       = azurerm_eventhub.eventhub.name
  resource_group_name = var.resource_group_name
}

resource "azurerm_eventhub_authorization_rule" "logstash" {
  name                = "logstashAccessKey${var.suffix}"
  namespace_name      = azurerm_eventhub_namespace.eventhub_namespace.name
  eventhub_name       = azurerm_eventhub.eventhub.name
  resource_group_name = var.resource_group_name
  listen              = true
  send                = false
  manage              = false
}

resource "azurerm_eventhub_authorization_rule" "log" {
  name                = "logAccessKey${var.suffix}"
  namespace_name      = azurerm_eventhub_namespace.eventhub_namespace.name
  eventhub_name       = azurerm_eventhub.eventhub.name
  resource_group_name = var.resource_group_name
  listen              = false
  send                = true
  manage              = false
}

resource "azurerm_storage_account" "logstashStorage" {
  name                      = var.logstashStorageName
  resource_group_name       = var.resource_group_name
  location                  = var.location
  account_kind              = "StorageV2"
  min_tls_version           = "TLS1_2"
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  access_tier               = "Hot"
  enable_https_traffic_only = true
  tags                      = var.tags
  allow_nested_items_to_be_public  = false
  network_rules {
    default_action             = "Deny"
    ip_rules                   = var.allowed_ips
    bypass                     = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids = [var.m_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet]
  }
}

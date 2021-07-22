data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "small_exchange_set_kv" {
  name                        = lower("${var.service_name}-${var.env_name}-sxs-kv")
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = true
  tenant_id                   = var.tenant_id

  sku_name = "standard"

  network_acls {
    default_action             = "Deny"
    bypass                     = "AzureServices"
    ip_rules                   = var.allowed_ips
    virtual_network_subnet_ids = concat(var.small_exchange_set_subnets, [var.agent_subnet])
  }

  tags = var.tags

}

resource "azurerm_key_vault_access_policy" "small_exchange_set_kv_access_terraform" {
  key_vault_id = azurerm_key_vault.small_exchange_set_kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  key_permissions = [
    "create",
    "get",
  ]

  secret_permissions = [
    "set",
    "get",
    "delete",
    "recover",
    "purge"
  ]
}

resource "azurerm_key_vault_access_policy" "small_exchange_set_kv_read_access" {
  for_each     = var.read_access_objects
  key_vault_id = azurerm_key_vault.small_exchange_set_kv.id
  tenant_id    = var.tenant_id
  object_id    = each.value

  key_permissions = [
    "list",
    "get",
  ]

  secret_permissions = [
    "list",
    "get"
  ]
}

resource "azurerm_key_vault_secret" "small_exchange_set_passed_in_secrets" {
  for_each     = var.small_exchange_set_secrets
  name         = each.key
  value        = each.value
  key_vault_id = azurerm_key_vault.small_exchange_set_kv.id
  tags         = var.tags

  depends_on = [azurerm_key_vault_access_policy.small_exchange_set_kv_access_terraform]
}

#Medium exchange set

resource "azurerm_key_vault" "medium_exchange_set_kv" {
  name                        = lower("${var.service_name}-${var.env_name}-mxs-kv")
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = true
  tenant_id                   = var.tenant_id

  sku_name = "standard"

  network_acls {
    default_action             = "Deny"
    bypass                     = "AzureServices"
    ip_rules                   = var.allowed_ips
    virtual_network_subnet_ids = concat(var.medium_exchange_set_subnets, [var.agent_subnet])
  }

  tags = var.tags

}

resource "azurerm_key_vault_access_policy" "medium_exchange_set_kv_access_terraform" {
  key_vault_id = azurerm_key_vault.medium_exchange_set_kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  key_permissions = [
    "create",
    "get",
  ]

  secret_permissions = [
    "set",
    "get",
    "delete",
    "recover",
    "purge"
  ]
}

resource "azurerm_key_vault_access_policy" "medium_exchange_set_kv_read_access" {
  for_each     = var.read_access_objects
  key_vault_id = azurerm_key_vault.medium_exchange_set_kv.id
  tenant_id    = var.tenant_id
  object_id    = each.value

  key_permissions = [
    "list",
    "get",
  ]

  secret_permissions = [
    "list",
    "get"
  ]
}

resource "azurerm_key_vault_secret" "medium_exchange_set_passed_in_secrets" {
  for_each     = var.medium_exchange_set_secrets
  name         = each.key
  value        = each.value
  key_vault_id = azurerm_key_vault.medium_exchange_set_kv.id
  tags         = var.tags

  depends_on = [azurerm_key_vault_access_policy.medium_exchange_set_kv_access_terraform]
}

#Large exchange set
resource "azurerm_key_vault" "large_exchange_set_kv" {
  name                        = lower("${var.service_name}-${var.env_name}-lxs-kv")
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = true
  tenant_id                   = var.tenant_id

  sku_name = "standard"

  network_acls {
    default_action             = "Deny"
    bypass                     = "AzureServices"
    ip_rules                   = var.allowed_ips
    virtual_network_subnet_ids = concat(var.large_exchange_set_subnets, [var.agent_subnet])
  }

  tags = var.tags

}

resource "azurerm_key_vault_access_policy" "large_exchange_set_kv_access_terraform" {
  key_vault_id = azurerm_key_vault.large_exchange_set_kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  key_permissions = [
    "create",
    "get",
  ]

  secret_permissions = [
    "set",
    "get",
    "delete",
    "recover",
    "purge"
  ]
}

resource "azurerm_key_vault_access_policy" "large_exchange_set_kv_read_access" {
  for_each     = var.read_access_objects
  key_vault_id = azurerm_key_vault.large_exchange_set_kv.id
  tenant_id    = var.tenant_id
  object_id    = each.value

  key_permissions = [
    "list",
    "get",
  ]

  secret_permissions = [
    "list",
    "get"
  ]
}

resource "azurerm_key_vault_secret" "large_exchange_set_passed_in_secrets" {
  for_each     = var.large_exchange_set_secrets
  name         = each.key
  value        = each.value
  key_vault_id = azurerm_key_vault.large_exchange_set_kv.id
  tags         = var.tags

  depends_on = [azurerm_key_vault_access_policy.large_exchange_set_kv_access_terraform]
}
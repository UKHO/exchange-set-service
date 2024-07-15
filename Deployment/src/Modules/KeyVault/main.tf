data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv" {
  name                        = var.name
  location                    = var.location
  resource_group_name         = var.resource_group_name
  enabled_for_disk_encryption = true
  tenant_id                   = var.tenant_id

  sku_name = "standard"

  network_acls {
    default_action             = "Deny"
    bypass                     = "AzureServices"
    ip_rules                   = var.allowed_ips
    virtual_network_subnet_ids = [var.agent_2204_subnet,var.agent_prd_subnet]
  }

  tags = var.tags

}

#access policy for terraform script service account
resource "azurerm_key_vault_access_policy" "kv_access_terraform" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  key_permissions = [
    "Create",
    "Get",
  ]

  secret_permissions = [
    "Set",
    "Get",
    "Delete",
    "Recover",
    "Purge"
  ]
}

#access policy for read access (app service)
resource "azurerm_key_vault_access_policy" "kv_read_access" {
  for_each     = var.read_access_objects
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = var.tenant_id
  object_id    = each.value

  key_permissions = [
    "List",
    "Get",
  ]

  secret_permissions = [
    "List",
    "Get"
  ]
}

resource "azurerm_key_vault_secret" "passed_in_secrets" {
  count        = length(var.secrets)
  name         = keys(var.secrets)[count.index]
  value        = values(var.secrets)[count.index]
  key_vault_id = azurerm_key_vault.kv.id
  tags         = var.tags

  depends_on = [azurerm_key_vault_access_policy.kv_access_terraform]
}

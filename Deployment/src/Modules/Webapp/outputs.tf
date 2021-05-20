output "webapp_service" {
  value = azurerm_app_service.webapp_service
}

output "web_app_object_id" {
  value = azurerm_app_service.webapp_service.identity.0.principal_id
}

output "web_app_tenant_id" {
  value = azurerm_app_service.webapp_service.identity.0.tenant_id
}

output "possible_outbound_ip_addresses" {
  value = azurerm_app_service.webapp_service.possible_outbound_ip_addresses
}

output "default_site_hostname" {
  value = azurerm_app_service.webapp_service.default_site_hostname
}

output "webapp_service" {
  value = azurerm_app_service.webapp_service
}

output "default_site_hostname" {
  value = azurerm_app_service.webapp_service.default_site_hostname
}

output "slot_name" {
  value = azurerm_app_service_slot.staging.name
}

output "slot_default_site_hostname" {
  value = azurerm_app_service_slot.staging.default_site_hostname
}

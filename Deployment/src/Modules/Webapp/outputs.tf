output "webapp_service" {
  value = azurerm_windows_web_app.webapp_service
}

output "default_site_hostname" {
  value = azurerm_windows_web_app.webapp_service.default_hostname
}

output "slot_name" {
  value = azurerm_windows_web_app_slot.staging.name
}

output "slot_default_site_hostname" {
  value = azurerm_windows_web_app_slot.staging.default_hostname
}

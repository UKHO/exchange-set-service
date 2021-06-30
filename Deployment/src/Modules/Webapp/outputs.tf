output "webapp_service" {
  value = azurerm_app_service.webapp_service
}

output "default_site_hostname" {
  value = azurerm_app_service.webapp_service.default_site_hostname
}

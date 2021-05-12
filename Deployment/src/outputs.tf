output "web_app_name" {
value = local.web_app_name
}

output "web_app_url" {
value = "https://${module.webapp_service.default_site_hostname}"
}
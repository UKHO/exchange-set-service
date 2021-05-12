output "web_app_name" {
value = local.web_app_name
}

output "web_app_url" {
value = module.webapp.default_site_hostname
}
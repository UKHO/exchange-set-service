module "exchange_set_service" {
  source							= "./Modules/ExchangeSetService"
  apim_service_name					= var.apim_name
  resource_group_name				= var.apim_rg
  env_name							= local.env_name
  apim_api_path						= lower("${local.service_name}-${local.env_name}")
  apim_api_service_url				= var.backend_url
  apim_group_name					= local.env_name == "prod" ? var.group_name : "${var.group_name} ${local.env_name}"
  apim_ess_product_name				= local.env_name == "prod" ? var.product_name : "${var.product_name} ${local.env_name}"
  apim_ess_product_description		= var.product_description
  apim_api_name						= local.env_name == "prod" ? var.api_name : "${var.api_name} ${local.env_name}"
  apim_api_description				= var.api_description
  apim_api_openapi					= file("${path.module}/exchangeSetService_OpenApi_definition.yaml")
}
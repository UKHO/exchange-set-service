module "exchange_set_service" {
  source							= "./ExchangeSetService"
  apim_service_name					= var.apim_service_name
  resource_group_name				= var.resource_group_name
  env_name							= var.env_name
  apim_api_path						= lower("${var.service_name}-${var.env_name})
  apim_api_url						= var.backend_url
  apim_group_name					= "Exchange Set Service"
  apim_fss_product_name				= "File Share Service"
  apim_fss_product_description		= "The File Share Service provides APIs to search for and download product data files."
  apim_fss_ui_product_name			= "File Share Service UI"
  apim_fss_ui_product_description	= "The File Share Service provides APIs to search for and download product data files using a UI."
  apim_api_name						= "File Share Service"
  apim_api_description				= "The File Share Service provides the ability to search for and download product data files."
  apim_api_openapi					= file("${path.module}/../../file-share-service.openApi-public.yaml")
}
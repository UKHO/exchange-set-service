module "exchange_set_service" {
  source							 = "./Modules/ExchangeSetService"
  apim_name							 = var.apim_name
  apim_rg							 = var.apim_rg
  env_name							 = local.env_name
  apim_api_path						 = local.apim_api_path
  apim_api_backend_url				 = var.apim_api_backend_url
  apim_group_name					 = local.group_name
  apim_group_description			 = var.group_description
  apim_ess_product_name				 = local.product_name
  apim_ess_product_description		 = var.product_description
  apim_api_name						 = local.api_name
  apim_api_description				 = var.api_description
  apim_api_openapi					 = local.apim_api_openapi
  product_rate_limit				 = var.product_rate_limit
  product_quota						 = var.product_quota
  client_credentials_operation_id    = var.client_credentials_operation_id
  client_credentials_tenant_id       = var.client_credentials_tenant_id
  client_credentials_scope           = var.client_credentials_scope
  
  apim_ess_ui_product_name           = local.ui_product_name
  ess_ui_product_call_limit			 = var.ess_ui_product_call_limit
  ess_ui_product_call_renewal_period = var.ess_ui_product_call_renewal_period
  ess_ui_product_daily_quota_limit	 = var.ess_ui_product_daily_quota_limit
  ess_b2c_token_issuer               = var.b2c_token_issuer
  ess_b2c_client_id                  = var.b2c_client_id
  cors_origins                       = local.cors_origins
  
  policy_rewrite_from_gateway        = var.policy_rewrite_from_gateway
  policy_rewrite_to_gateway          = var.policy_rewrite_to_gateway
}
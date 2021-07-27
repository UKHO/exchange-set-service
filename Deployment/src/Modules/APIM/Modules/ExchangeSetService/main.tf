data "azurerm_resource_group" "rg" {
  name = var.apim_rg
}

data "azurerm_api_management" "apim_instance" {
  name                = var.apim_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# Create apim group
resource "azurerm_api_management_group" "ess_management_group" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  name                = lower(replace(var.apim_group_name, " ", "-"))
  display_name        = title(var.apim_group_name)
  description         = var.apim_group_description
}

# Create ESS Product
resource "azurerm_api_management_product" "ess_product" {
  resource_group_name   = data.azurerm_resource_group.rg.name
  api_management_name   = data.azurerm_api_management.apim_instance.name
  product_id            = lower(replace(var.apim_ess_product_name, " ", "-"))
  display_name          = title(var.apim_ess_product_name)
  description           = var.apim_ess_product_description
  subscription_required = true
  approval_required     = true
  published             = true
  subscriptions_limit   = 1
}

# ESS product-Group mapping
resource "azurerm_api_management_product_group" "product_group_mappping" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  product_id          = azurerm_api_management_product.ess_product.product_id
  group_name          = azurerm_api_management_group.ess_management_group.name
}

# Create ESS API
resource "azurerm_api_management_api" "ess_api" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  name                = lower(replace(var.apim_api_name, " ", "-"))
  display_name        = var.apim_api_name
  description         = var.apim_api_description
  revision            = "1"
  path                = var.apim_api_path
  protocols           = ["https"]
  service_url         = var.apim_api_backend_url

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi"
    content_value  = var.apim_api_openapi
  }
}

# Add ESS API to ESS Product
resource "azurerm_api_management_product_api" "ess_product_api_mapping" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  api_name            = azurerm_api_management_api.ess_api.name
  product_id          = azurerm_api_management_product.ess_product.product_id
}

#Product quota and throttle policy
resource "azurerm_api_management_product_policy" "ess_product_policy" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  product_id          = azurerm_api_management_product.ess_product.product_id
  depends_on          = [azurerm_api_management_product.ess_product, azurerm_api_management_product_api.ess_product_api_mapping]

  xml_content = <<XML
	<policies>
	  <inbound>
		 <rate-limit calls="${var.product_rate_limit.calls}" renewal-period="${var.product_rate_limit.renewal-period}" retry-after-header-name="retry-after" remaining-calls-header-name="remaining-calls" />
		 <quota calls="${var.product_quota.calls}" renewal-period="${var.product_quota.renewal-period}" />
		 <base />
	  </inbound>
	</policies>
	XML
}
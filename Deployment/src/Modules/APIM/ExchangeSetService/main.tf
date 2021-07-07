data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_api_management" "apim_instance" {
  name                = var.apim_service_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# Create apim group
resource "azurerm_api_management_group" "ess_management_group" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  name                = lower(replace(trimsuffix("${var.apim_group_name} ${var.EnvSuffix[lower(var.env)]}","  "), " ", "-"))
  display_name        = "${var.apim_group_name} ${var.EnvSuffix[lower(var.env)]}"
  description         = "Management group for users with access to the ${var.env} File Share Service."
}

# Create FSS Product
resource "azurerm_api_management_product" "fss_product" {
  resource_group_name   = var.resource_group_name
  api_management_name   = var.apim_service_name
  product_id            = lower(replace(trimsuffix("${var.apim_fss_product_name} ${var.EnvSuffix[lower(var.env)]}","  "), " ", "-"))
  display_name          = "${var.apim_fss_product_name} ${var.EnvSuffix[lower(var.env)]}"
  description           = var.apim_fss_product_description
  subscription_required = true
  approval_required     = true
  published             = true
  subscriptions_limit   = 1
}

# FSS product-Group mapping
resource "azurerm_api_management_product_group" "product_group_mappping" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  product_id          = azurerm_api_management_product.fss_product.product_id
  group_name          = azurerm_api_management_group.fss_management_group.name
}

# Create FSS API
resource "azurerm_api_management_api" "fss_api" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  name                = lower(replace(trimsuffix("${var.apim_api_name} ${var.EnvSuffix[lower(var.env)]}","  "), " ", "-"))
  display_name        = "${var.apim_api_name} ${var.EnvSuffix[lower(var.env)]}"
  description         = var.apim_api_description
  revision            = "1"
  path                = var.apim_api_path
  protocols           = ["https"]
  service_url         = var.apim_api_service_url

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi"
    content_value  = var.apim_api_openapi
  }
}

# Add FSS API to FSS Product
resource "azurerm_api_management_product_api" "fss_product_api_mapping" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  api_name            = azurerm_api_management_api.fss_api.name
  product_id          = azurerm_api_management_product.fss_product.product_id
}

#Product quota and throttle policy
resource "azurerm_api_management_product_policy" "fss_product_policy" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  product_id          = azurerm_api_management_product.fss_product.product_id
  depends_on          = [azurerm_api_management_product.fss_product, azurerm_api_management_product_api.fss_product_api_mapping]

  xml_content = <<XML
	<policies>
	  <inbound>
		 <rate-limit calls="10" renewal-period="1"/>
		 <quota calls="10000" renewal-period="86400" />
		 <base />
	  </inbound>
	</policies>
	XML
}

# Create FSS UI Product
resource "azurerm_api_management_product" "fss_ui_product" {
  resource_group_name   = var.resource_group_name
  api_management_name   = var.apim_service_name
  product_id            = lower(replace(trimsuffix("${var.apim_fss_ui_product_name} ${var.EnvSuffix[lower(var.env)]}","  "), " ", "-"))
  display_name          = "${var.apim_fss_ui_product_name} ${var.EnvSuffix[lower(var.env)]}"
  description           = var.apim_fss_ui_product_description
  subscription_required = false
  approval_required     = false
  published             = true  
}

# FSS product-Group mapping
resource "azurerm_api_management_product_group" "ui_product_group_mappping" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  product_id          = azurerm_api_management_product.fss_ui_product.product_id
  group_name          = azurerm_api_management_group.fss_management_group.name
}

# Add FSS API to FSS UI Product
resource "azurerm_api_management_product_api" "fss_ui_product_api_mapping" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  api_name            = azurerm_api_management_api.fss_api.name
  product_id          = azurerm_api_management_product.fss_ui_product.product_id
}

#Product quota and throttle policy
resource "azurerm_api_management_product_policy" "fss_ui_product_policy" {
  resource_group_name = var.resource_group_name
  api_management_name = var.apim_service_name
  product_id          = azurerm_api_management_product.fss_ui_product.product_id
  depends_on          = [azurerm_api_management_product.fss_ui_product, azurerm_api_management_product_api.fss_ui_product_api_mapping]

  xml_content = <<XML
	<policies>
	  <inbound>
        <base />
        <validate-jwt header-name="Authorization" failed-validation-error-message="Auth token is missing or invalid" require-scheme="Bearer" output-token-variable-name="jwt">
            <openid-config url="https://login.microsoftonline.com/${var.fss_tennant_id}/.well-known/openid-configuration" />
        </validate-jwt>
        <choose>
            <when condition="@(!((Jwt)context.Variables["jwt"]).Claims.ContainsKey("oid"))">
                <return-response>
                    <set-status code="403" reason="Forbidden" />
                    <set-body>"oid" claim missing in token</set-body>
                </return-response>
            </when>
        </choose>
        <set-variable name="oid" value="@(((Jwt)context.Variables["jwt"]).Claims["oid"][0])" />
        <rate-limit-by-key calls="5" renewal-period="60" counter-key="@((string)context.Variables["oid"])" increment-condition="@(context.Response.StatusCode == 200)" retry-after-header-name="retry-after" remaining-calls-header-name="remaining-calls" />
        <quota calls="1000" renewal-period="86400" />
    </inbound>
	</policies>
	XML
}
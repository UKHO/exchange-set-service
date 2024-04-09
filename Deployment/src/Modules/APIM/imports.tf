# This will import an existing APIM if Terraform is not already aware of it.
# This was used to import following the migration of APIMs from TPE to UKHO tenants.

data "azurerm_api_management" "apim_instance" {
  name                = var.apim_name
  resource_group_name = var.apim_rg
}

locals {
  import_group_name        = lower(replace("${local.group_name}", " ", "-"))
  import_ess_product_id    = lower(replace("${local.product_name}", " ", "-"))
  import_api_name          = lower(replace("${local.api_name}", " ", "-"))
  import_operations_name   = "${var.client_credentials_operation_id}"
  import_ess_ui_product_id = lower(replace("${local.ui_product_name}", " ", "-"))
}

import {
  to = module.exchange_set_service.azurerm_api_management_group.ess_management_group
  id = "${data.azurerm_api_management.apim_instance.id}/groups/${local.import_group_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product.ess_product
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_product_id}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product_group.product_group_mappping
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_product_id}/groups/${local.import_group_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_api.ess_api
  id = "${data.azurerm_api_management.apim_instance.id}/apis/${local.import_api_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product_api.ess_product_api_mapping
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_product_id}/apis/${local.import_api_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product_policy.ess_product_policy
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_product_id}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_api_operation_policy.client_credentials_token_operation_policy
  id = "${data.azurerm_api_management.apim_instance.id}/apis/${local.import_api_name}/operations/${local.import_operations_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product.ess_ui_product
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_ui_product_id}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product_group.ess_ui_product_group_mappping
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_ui_product_id}/groups/${local.import_group_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product_api.ess_ui_product_api_mapping
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_ui_product_id}/apis/${local.import_api_name}"
}

import {
  to = module.exchange_set_service.azurerm_api_management_product_policy.ess_ui_product_policy
  id = "${data.azurerm_api_management.apim_instance.id}/products/${local.import_ess_ui_product_id}"
}

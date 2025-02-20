variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "app_settings" {
  type = map(string)
}

variable "tags" {
}

variable "small_exchange_set_subnets" {
}

variable "medium_exchange_set_subnets" {
}

variable "large_exchange_set_subnets" {
}

variable "exchange_set_config" {
}

variable "user_assigned_identity" {
  type = string
}

variable "asp_control_sxs" {
  type = object({ sku = string, zoneRedundant = bool })
}

variable "asp_control_mxs" {
  type = object({ sku = string, zoneRedundant = bool })
}

variable "asp_control_lxs" {
  type = object({ sku = string, zoneRedundant = bool })
}

variable "asp_name_sxs" {
  type = list(string)
}

variable "asp_name_mxs" {
  type = list(string)
}

variable "asp_name_lxs" {
  type = list(string)
}

variable "as_name_sxs" {
  type = list(string)
}

variable "as_name_mxs" {
  type = list(string)
}

variable "as_name_lxs" {
  type = list(string)
}

variable "suffix" {
  default = ""
}

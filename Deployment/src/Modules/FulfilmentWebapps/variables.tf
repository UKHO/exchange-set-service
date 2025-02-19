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

variable "exchange_set_config"{

}

variable "service_name" {
  type = string
}

variable "env_name"{
  type = string
}

variable "user_assigned_identity" {
  type = string
}

variable "sku_sxs" {
  type = string
}

variable "sku_mxs" {
  type = string
}

variable "sku_lxs" {
  type = string
}

variable "medium_exchange_set_subnets" {
}

variable "large_exchange_set_subnets" {
}

locals {
  small_exchange_set_name = "${var.service_name}-${var.env_name}-sxs"
  medium_exchange_set_name = "${var.service_name}-${var.env_name}-mxs"
  large_exchange_set_name = "${var.service_name}-${var.env_name}-lxs"
}

variable "suffix" {
  default     = ""
}

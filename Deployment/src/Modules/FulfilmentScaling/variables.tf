variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tags" {
}

variable "exchange_set_config" {
}

variable "queue_resource_uri_sxs" {
  type = list(string)
}

variable "queue_resource_uri_mxs" {
  type = list(string)
}

variable "queue_resource_uri_lxs" {
  type = list(string)
}

variable "exchange_set_config" {
}

variable "asp" {
}

variable "asp_control_sxs" {
}

variable "asp_control_mxs" {
}

variable "asp_control_lxs" {
}

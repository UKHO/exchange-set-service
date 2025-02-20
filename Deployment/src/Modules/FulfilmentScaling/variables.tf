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






variable "asp_name" {
  type = string
}

variable "asp_id" {
  type = string
}

variable "auto_scale_in_threshold" {
  type = number
}

variable "auto_scale_out_threshold" {
  type = number
}

variable "auto_scale_max_instances" {
  type = number
}

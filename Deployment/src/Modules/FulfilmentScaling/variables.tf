variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tags" {

}

variable "exchange_set_config"{

}

variable "asp_name" {
  type = string
}

variable "asp_id" {
  type = string
}

variable "commit_queue_metric_resource_uri" {
  type = string
}

variable "env_name"{
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

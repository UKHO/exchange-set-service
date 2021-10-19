variable "service_name" {
  type = string
}

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

variable "app_service_sku" {

}

variable "env_name" {
  type = string
}

variable "user_assigned_identity" {
  type = string
}
variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "env_name" {
  type = string
}

variable "service_name"{
  type = string
}

variable "small_exchange_set_read_access_objects" {
  type = map(string)
}

variable "small_exchange_set_secrets" {
  type = map(string)
}

variable "tags" {

}

variable "allowed_ips" {

}

variable "small_exchange_set_subnets" {
}
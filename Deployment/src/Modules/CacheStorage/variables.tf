variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "env_name" {
  type  = string
}

variable "tags" {
}

variable "allowed_ips" {

}

variable "service_name" {
  type = string
}

variable "m_spoke_subnet" {
  type = string
}

variable "agent_subnet" {
  type = string
}

variable "small_exchange_set_subnets" {
}

variable "medium_exchange_set_subnets" {
}

variable "large_exchange_set_subnets" {
} 
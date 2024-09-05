variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "logstashStorageName"{
  type=string
}

variable "agent_2204_subnet" {
  type = string
}

variable "agent_prd_subnet" {
  type = string
}

variable "m_spoke_subnet" {
  type = string
}

variable "allowed_ips" {

}

variable "tags" {

}

variable "suffix" {
  default     = ""
}

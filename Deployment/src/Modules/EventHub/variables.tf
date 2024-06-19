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

variable "agent_subnet" {
  type = string
}

variable "m_spoke_subnet" {
  type = string
}

variable "allowed_ips" {

}

variable "tags" {

}

variable "name" {
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

variable "subnet_id" {
  type = string
}

variable "user_assigned_identity" {
  type = string
}

variable "asp_control_webapp" {

}

variable "allowed_ips" {

}

variable "front_door_id" {
  description = "The shared Front Door profile resource GUID (X-Azure-FDID). When set, Front Door traffic is allowed and all unmatched traffic is denied."
  type        = string
  default     = ""
}

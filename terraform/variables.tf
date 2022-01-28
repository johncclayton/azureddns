variable "spn-tenant-id" {
    type = string
}

variable "spn-secret-value" {
    type = string
}

variable "spn-client-id" {
    type = string
}

variable "prefix" {
    type = string
    default = "AzDNSRG-5"
}

variable "location" {
    type = string
    default = "switzerlandnorth"
}

variable "environment" {
    type = string
    default = "prod"
}

variable "functionapp" {
    type = string
    default = "./deploy/functionapp.zip"
}

resource "random_string" "storage_name" {
    length = 24
    upper = false
    lower = true
    number = true
    special = false
}
& cls
& terraform init -backend-config="resource_group_name=FileShareSerDevP4052-RG" -backend-config="storage_account_name=fileshareserdevp4052" -backend-config="key=terraform.deployment.tfplan"
& terraform workspace select dev 
& terraform validate
& terraform plan -var 'resource_group_name=FileShareService' -out "terraform.deployment.tfplan"
#& terraform apply terraform.deployment.tfplan
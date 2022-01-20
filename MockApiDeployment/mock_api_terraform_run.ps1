param (
    [Parameter(Mandatory = $true)] [string] $deploymentResourceGroupName,
    [Parameter(Mandatory = $true)] [string] $deploymentStorageAccountName,
    [Parameter(Mandatory = $true)] [string] $workSpace
)

cd $env:AGENT_BUILDDIRECTORY/mockapiterraformartifact/src

Write-output "Executing terraform scripts for deployment of mock api in $workSpace enviroment"
terraform init -backend-config="resource_group_name=$deploymentResourceGroupName" -backend-config="storage_account_name=$deploymentStorageAccountName" -backend-config="key=mockapiterraform.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform initialization"; throw "Error" }

Write-output "Selecting workspace"

$ErrorActionPreference = 'SilentlyContinue'
terraform workspace new $WorkSpace 2>&1 > $null
$ErrorActionPreference = 'Continue'

terraform workspace select $workSpace
if ( !$? ) { echo "Error while selecting workspace"; throw "Error" }

Write-output "Validating terraform"
terraform validate
if ( !$? ) { echo "Something went wrong during terraform validation" ; throw "Error" }

Write-output "Execute Terraform plan"
terraform plan -out "mockapiterraform.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform plan" ; throw "Error" }

Write-output "Executing terraform apply"
terraform apply  "mockapiterraform.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform apply" ; throw "Error" }

Write-output "Terraform output as json"
$terraformOutput = terraform output -json | ConvertFrom-Json

write-output "Set JSON output into pipeline variables"
Write-Host "##vso[task.setvariable variable=essWebAppName]$($terraformOutput.ess_webappname.value)"
Write-Host "##vso[task.setvariable variable=mockWebAppName]$($terraformOutput.mock_webappname.value)"
Write-Host "##vso[task.setvariable variable=essFulfilmentWebAppname]$($terraformOutput.ess_fulfilment_webappname.value)"
Write-Host "##vso[task.setvariable variable=EssApiUrl]$($terraformOutput.ess_web_app_url.value)"
Write-Host "##vso[task.setvariable variable=webAppResourceGroup]$($terraformOutput.webapp_rg.value)"
Write-Host "##vso[task.setvariable variable=KeyVaultSettings.ServiceUri]$($terraformOutput.keyvault_uri.value)"
Write-Host "##vso[task.setvariable variable=ESSManagedIdentity.ClientId]$($terraformOutput.ess_managed_user_identity_client_id.value)"
Write-Host "##vso[task.setvariable variable=FileShareService.BaseUrl]$($terraformOutput.scs_fss_mock_web_app_url.value)"
Write-Host "##vso[task.setvariable variable=SalesCatalogue.BaseUrl]$($terraformOutput.scs_fss_mock_web_app_url.value)"
Write-Host "##vso[task.setvariable variable=ESSFulfilmentStorageConfiguration.QueueName]$($terraformOutput.storage_account_queue_name.value)"
Write-Host "##vso[task.setvariable variable=CacheConfiguration.CacheStorageConnectionString;issecret=true]$($terraformOutput.cache_qc_storage_connection_string.value)"

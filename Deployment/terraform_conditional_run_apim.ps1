param (
    [Parameter(Mandatory = $true)] [string] $deploymentResourceGroupName,
    [Parameter(Mandatory = $true)] [string] $deploymentStorageAccountName,
    [Parameter(Mandatory = $true)] [string] $workSpace,
    [Parameter(Mandatory = $true)] [boolean] $continueEvenIfResourcesAreGettingDestroyed
)

cd $env:AGENT_BUILDDIRECTORY/terraformartifact/src/Modules/APIM/

Write-output "Executing terraform scripts for APIM deployment in $workSpace enviroment..."

terraform init -backend-config="resource_group_name=$deploymentResourceGroupName" -backend-config="storage_account_name=$deploymentStorageAccountName" -backend-config="key=terraform.ess.apim.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform initialization"; throw "Error" }

Write-output "Selecting workspace..."

$ErrorActionPreference = 'SilentlyContinue'
terraform workspace new $WorkSpace 2>&1 > $null
$ErrorActionPreference = 'Continue'

terraform workspace select $workSpace
if ( !$? ) { echo "Error while selecting workspace"; throw "Error" }

Write-output "Validating terraform..."
terraform validate
if ( !$? ) { echo "Something went wrong during terraform validation" ; throw "Error" }

Write-output "Execute Terraform plan..."
terraform plan -out "terraform.ess.apim.deployment.tfplan" | tee terraform_output.txt
if ( !$? ) { echo "Something went wrong during terraform plan" ; throw "Error" }

$totalDestroyLines=(Get-Content -Path terraform_output.txt | Select-String -Pattern "destroy" -CaseSensitive |  where {$_ -ne ""}).length
if($totalDestroyLines -ge 2) 
{
    write-Host("Terraform is destroying some resources, please verify...................")
    if ( !$ContinueEvenIfResourcesAreGettingDestroyed) 
    {
        write-Host("exiting...................")
        Write-Output $_
        exit 1
    }
    write-host("Continue executing terraform apply - as continueEvenIfResourcesAreGettingDestroyed param is set to true in pipeline...")
}

Write-output "Executing terraform apply..."
terraform apply  "terraform.ess.apim.deployment.tfplan"
if ( !$? ) { echo "Something went wrong during terraform apply" ; throw "Error" }
param (
    [Parameter(Mandatory = $true)] [string] $DeploymentResourceGroupName,
    [Parameter(Mandatory = $true)] [string] $DeploymentStorageAccountName,
    [Parameter(Mandatory = $true)] [string] $WorkSpace,
    [Parameter(Mandatory = $true)] [string] $Terraformapply
)

cd $env:AGENT_BUILDDIRECTORY/terraformartifact/src

Write-output "Executing terraform scripts for deployment in $WorkSpace Enviroment"
terraform init -backend-config="resource_group_name=$DeploymentResourceGroupName" -backend-config="storage_account_name=$DeploymentStorageAccountName" -backend-config="key=terraform.deployment.tfplan"
if ( !$? ) { echo "something went wrong during terraform initialization"; throw "Error" }

Write-output "Selecting workspace"
terraform workspace select $WorkSpace
if ( !$? ) {
    echo "Error while selecting workspace trying to create it"

    terraform workspace new $WorkSpace
    if ( !$? ){ echo "something went wrong during creating workspace" ; throw "Error" }
}

Write-output "Validating terraform"
terraform validate
if ( !$? ) { echo "something went wrong during terraform validation" ; throw "Error" }

Write-output "Execute Terraform plan"
terraform plan -out "terraform.deployment.tfplan" -no-color | tee terraform_output.txt
if ( !$? ) { echo "something went wrong during terraform plan" ; throw "Error" }

$totaldestroylines=(Get-Content -Path terraform_output.txt | Select-String -Pattern "destroy" -CaseSensitive |  where {$_ -ne ""}).length
   if($totaldestroylines -ge 2) {
      write-Host("terraform is destroying some resources, please Verify...................")
      $destroyingresources = "true"
      Write-Host "##vso[task.setvariable variable=DestroyResource]$destroyingresources"
         if ( $Terraformapply -ceq "False" ) {
            write-Host("exiting...................")
            Write-Output $_
            exit 1
         else {
            write-host("Continue executing terraform apply")
            $destroyingresources = "false"
            Write-Host "##vso[task.setvariable variable=DestroyResource]$destroyingresources"
         }
      }
   }

Write-output "Executing terraform apply"
terraform apply  "terraform.deployment.tfplan"
if ( !$? ) { echo "something went wrong during terraform apply" ; throw "Error" }

Write-output "terraform output as json"
$terraformOutput = terraform output -json | ConvertFrom-Json

echo $terraformOutput

write-output "Set JSON output into Pipeline Variables"
Write-Host "##vso[task.setvariable variable=WEB_APP_NAME]$($terraformOutput.web_app_name.value)"
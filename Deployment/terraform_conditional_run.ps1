param (
    [Parameter(Mandatory = $true)] [string] $deploymentResourceGroupName,
    [Parameter(Mandatory = $true)] [string] $deploymentStorageAccountName,
    [Parameter(Mandatory = $true)] [string] $workSpace,
    [Parameter(Mandatory = $true)] [boolean] $continueEvenIfResourcesAreGettingDestroyed,
    [Parameter(Mandatory = $true)] [string] $terraformJsonOutputFile,
    [Parameter(Mandatory = $true)] [string] $elasticApmServerUrl,
    [Parameter(Mandatory = $true)] [string] $elasticApmApiKey
)

cd $env:AGENT_BUILDDIRECTORY/terraformartifact/src

terraform --version

Write-Output "Executing terraform scripts for deployment in $workSpace environment"

terraform init -upgrade `
    -backend-config="resource_group_name=$deploymentResourceGroupName" `
    -backend-config="storage_account_name=$deploymentStorageAccountName" `
    -backend-config="key=terraform.deployment.tfplan"

if (!$?) {
    Write-Output "Something went wrong during terraform initialization"
    throw "Error"
}


# -------------------------------------------------------------------------
# Select Terraform workspace
# -------------------------------------------------------------------------

Write-Output "Selecting workspace"

$ErrorActionPreference = 'SilentlyContinue'
terraform workspace new $workSpace 2>&1 > $null
$ErrorActionPreference = 'Continue'

terraform workspace select $workSpace

if (!$?) {
    Write-Output "Error while selecting workspace"
    throw "Error"
}


# -------------------------------------------------------------------------
# Legacy Azure Dashboard state migration
#
# AzureRM v4 no longer has a schema for azurerm_dashboard.
# If the old resource exists in state, remove the state binding only.
# The actual Azure dashboard is NOT deleted.
#
# The existing Terraform import block will subsequently import the
# dashboard as azurerm_portal_dashboard during plan/apply.
# -------------------------------------------------------------------------

Write-Output "Checking whether legacy Azure Dashboard state migration is required"

$legacyDashboardAddress = "module.azure-dashboard.azurerm_dashboard.azure-dashboard"
$newDashboardAddress    = "module.azure-dashboard.azurerm_portal_dashboard.azure-dashboard"


# Confirm which workspace we are about to modify

$currentWorkspace = terraform workspace show

if (!$?) {
    Write-Output "Unable to determine the current Terraform workspace"
    throw "Error"
}

$currentWorkspace = $currentWorkspace.Trim()

Write-Output "Current Terraform workspace: $currentWorkspace"

if ($currentWorkspace -ne $workSpace) {
    Write-Output "Expected workspace '$workSpace' but Terraform reports '$currentWorkspace'"
    throw "Error"
}


# Read current Terraform state

$stateResources = @(terraform state list)

if (!$?) {
    Write-Output "Unable to read Terraform state"
    throw "Error"
}


$legacyDashboardExists = $stateResources -contains $legacyDashboardAddress
$newDashboardExists    = $stateResources -contains $newDashboardAddress


Write-Output "Legacy azurerm_dashboard present in state: $legacyDashboardExists"
Write-Output "New azurerm_portal_dashboard present in state: $newDashboardExists"


# Handle the four possible migration states

if ($legacyDashboardExists -and $newDashboardExists) {

    Write-Output "ERROR: Both the legacy and new dashboard resources exist in Terraform state."
    Write-Output "Legacy address: $legacyDashboardAddress"
    Write-Output "New address:    $newDashboardAddress"
    Write-Output "Automatic migration has been stopped to prevent an unsafe state change."

    throw "Both legacy and new dashboard resources exist in Terraform state"
}
elseif ($legacyDashboardExists) {

    Write-Output "Legacy Azure Dashboard detected in Terraform state."
    Write-Output "Removing legacy Terraform state binding."
    Write-Output "The Azure Dashboard resource itself will NOT be destroyed."

    terraform state rm `
        -lock-timeout=5m `
        $legacyDashboardAddress

    if (!$?) {
        Write-Output "Failed to remove legacy Azure Dashboard from Terraform state"
        throw "Error"
    }

    Write-Output "Legacy Azure Dashboard state binding removed successfully."


    # Verify that the legacy resource has actually disappeared

    Write-Output "Verifying legacy Dashboard state removal"

    $stateResourcesAfterRemoval = @(terraform state list)

    if (!$?) {
        Write-Output "Unable to verify Terraform state after Dashboard migration"
        throw "Error"
    }

    if ($stateResourcesAfterRemoval -contains $legacyDashboardAddress) {
        Write-Output "Legacy Dashboard is still present in Terraform state after state rm"
        throw "Error"
    }

    Write-Output "Legacy Dashboard state removal verified successfully."
}
elseif ($newDashboardExists) {

    Write-Output "Azure Dashboard has already been migrated to azurerm_portal_dashboard."
    Write-Output "No Dashboard state migration is required."
}
else {

    Write-Output "Legacy azurerm_dashboard is not present in Terraform state."
    Write-Output "azurerm_portal_dashboard is also not currently present in Terraform state."
    Write-Output "No state removal is required."
    Write-Output "The Terraform import block will handle the existing Azure Dashboard if applicable."
}


# -------------------------------------------------------------------------
# Normal Terraform validation
# -------------------------------------------------------------------------

Write-Output "Validating terraform"

terraform validate

if (!$?) {
    Write-Output "Something went wrong during terraform validation"
    throw "Error"
}


# -------------------------------------------------------------------------
# Terraform plan
# -------------------------------------------------------------------------

Write-Output "Execute Terraform plan"

terraform plan `
    -out "terraform.deployment.tfplan" `
    -var elastic_apm_server_url=$elasticApmServerUrl `
    -var elastic_apm_api_key=$elasticApmApiKey `
    -var suffix="" `
    -var storage_suffix="" |
    tee terraform_output.txt

if (!$?) {
    Write-Output "Something went wrong during terraform plan"
    throw "Error"
}


# -------------------------------------------------------------------------
# Detect planned destroys
# -------------------------------------------------------------------------

$totalDestroyLines = (
    Get-Content -Path terraform_output.txt |
    Select-String -Pattern "destroy" -CaseSensitive |
    Where-Object { $_ -ne "" }
).length

if ($totalDestroyLines -ge 2)
{
    Write-Host "Terraform is destroying some resources, please verify..................."

    if (!$continueEvenIfResourcesAreGettingDestroyed)
    {
        Write-Host "exiting..................."
        Write-Output $_
        exit 1
    }

    Write-Host "Continue executing terraform apply - as continueEvenIfResourcesAreGettingDestroyed param is set to true in pipeline"
}


# -------------------------------------------------------------------------
# Terraform apply
# -------------------------------------------------------------------------

Write-Output "Executing terraform apply"

terraform apply "terraform.deployment.tfplan"

if (!$?) {
    Write-Output "Something went wrong during terraform apply"
    throw "Error"
}


# -------------------------------------------------------------------------
# Terraform outputs
# -------------------------------------------------------------------------

Write-Output "Terraform output as json"

$terraformOutput = terraform output -json | ConvertFrom-Json


Write-Output "Set JSON output into pipeline variables"

Write-Host "##vso[task.setvariable variable=WEB_APP_NAME]$($terraformOutput.web_app_name.value)"
Write-Host "##vso[task.setvariable variable=EssApiUrl]$env:SERVICE_DNS_URL"
Write-Host "##vso[task.setvariable variable=KeyVaultSettings.ServiceUri]$($terraformOutput.keyvault_uri.value)"
Write-Host "##vso[task.setvariable variable=EssStorageAccountConnectionString;issecret=true]$($terraformOutput.storage_connection_string.value)"
Write-Host "##vso[task.setvariable variable=ESSManagedIdentity.ClientId]$($terraformOutput.ess_managed_user_identity_client_id.value)"
Write-Host "##vso[task.setvariable variable=RESOURCE_GROUP_NAME]$($terraformOutput.web_app_resource_group.value)"
Write-Host "##vso[task.setvariable variable=WEB_APP_SLOT_NAME]$($terraformOutput.web_app_slot_name.value)"
Write-Host "##vso[task.setvariable variable=WEB_APP_SLOT_HOST_NAME]$($terraformOutput.web_app_slot_default_site_hostname.value)"
Write-Host "##vso[task.setvariable variable=RESOURCEGROUPNAME;isOutput=true]$($terraformOutput.web_app_resource_group.value)"
Write-Host "##vso[task.setvariable variable=WEBAPPNAME;isOutput=true]$($terraformOutput.web_app_name.value)"
Write-Host "##vso[task.setvariable variable=EssURL;isOutput=true]$env:SERVICE_DNS_URL"
Write-Host "##vso[task.setvariable variable=small_exchange_set_webapps;isOutput=true]$env:small_exchange_set_webapps"
Write-Host "##vso[task.setvariable variable=medium_exchange_set_webapps;isOutput=true]$env:medium_exchange_set_webapps"
Write-Host "##vso[task.setvariable variable=large_exchange_set_webapps;isOutput=true]$env:large_exchange_set_webapps"


$terraformOutput |
    ConvertTo-Json -Depth 5 > $terraformJsonOutputFile

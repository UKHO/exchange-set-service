param (   
    [Parameter(Mandatory = $true)] [string] $resourcegroup,   
    [Parameter(Mandatory = $true)] [string] $webappname,
    [Parameter(Mandatory = $true)] [string[]] $fulfilmentwebappsname,
    [Parameter(Mandatory = $true)] [string] $aioenabled    
)

Write-Output "Set ESS API Configuration in appsetting..."
az webapp config appsettings set -g $resourcegroup -n $webappname --settings AioConfiguration:AioEnabled=$aioenabled
az webapp restart --name $webappname --resource-group $resourcegroup

Write-Output "Set Fulfilment Webjob Configuration in appsetting..."
foreach($webapp in $fulfilmentwebappsname)
{
  az webapp config appsettings set -g $resourcegroup -n $webapp --settings AioConfiguration:AioEnabled=$aioenabled
  az webapp restart --name $webapp --resource-group $resourcegroup
}
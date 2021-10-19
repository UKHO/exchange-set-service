param (
    [Parameter(Mandatory = $true)] [string] $essWebAppName,
    [Parameter(Mandatory = $true)] [string] $essApipackagePath,
    [Parameter(Mandatory = $true)] [string] $mockWebAppName,
    [Parameter(Mandatory = $true)] [string] $mockApipackagePath,
    [Parameter(Mandatory = $true)] [string] $essFulfilmentWebAppname,
    [Parameter(Mandatory = $true)] [string] $essFulfilmentpackagePath,
    [Parameter(Mandatory = $true)] [string] $webAppResourceGroup
)

echo "essApipackagePath : $essApipackagePath"
echo "mockApipackagePath : $mockApipackagePath"
echo "essFulfilmentpackagePath : $essFulfilmentpackagePath"
echo "essWebAppName : $essWebAppName"
echo "mockWebAppName : $mockWebAppName"
echo "essFulfilmentWebAppname : $essFulfilmentWebAppname"
echo "ResourceGroup : $webAppResourceGroup"

function DeployWebApp($webAppName, $package, $webAppRGroup){
    
    echo "Function DeployWebApp called with params $webAppName, $package, $webAppRGroup ..."

    az webapp deployment source config-zip -g $webAppRGroup -n $webAppName --src $package

    if ( !$? ) { echo "Error while deplying webapp $webAppName" ; throw $_ }
}


echo "Deploying ess api ..."
DeployWebApp $essWebAppName $essApipackagePath $webAppResourceGroup

if ( !$? ) { echo "Error while deploying ess api webapp" ; throw $_ }

echo "Deploying ess api done ..."

echo "Deploying mock api ..."
DeployWebApp $mockWebAppName $mockApipackagePath $webAppResourceGroup

if ( !$? ) { echo "Error while deploying mock api webapp" ; throw $_ }

echo "Deploying mock api done ..."

echo "Deploying ess fulfilment webjob ..."
DeployWebApp $essFulfilmentWebAppname $essFulfilmentpackagePath $webAppResourceGroup

if ( !$? ) { echo "Deploying ess fulfilment webjob" ; throw $_ }

echo "Deploying ess fulfilment webjob done ..."

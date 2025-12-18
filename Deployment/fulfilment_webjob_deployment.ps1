param (
    [Parameter(Mandatory = $true)] [string] $terraformJsonOutputFile,
    [Parameter(Mandatory = $true)] [string] $packagePath,
    [Parameter(Mandatory = $true)] [string] $packageName,
    [Parameter(Mandatory = $true)] [string] $exchangeSetSize
)

echo "terraformJsonOutputFile : $terraformJsonOutputFile"
echo "packagePath : $packagePath"
echo "packageName : $packageName"

function ReplaceQueueAndDeployWebApp($packagePath, $packageName, $exchangeSet, $webAppResourceGroup) {
    echo "Function ReplaceQueueAndDeployWebApp called with params $packagePath, $packageName, $exchangeSet, $webAppResourceGroup"
    $exchangeSetWebapps = @()
    $keyVaultUri = ""

    if ( $exchangeSet -eq "small" ) {
        $exchangeSetWebapps = $terraformOutput.small_exchange_set_webapps.value
        $keyVaultUri = $terraformOutput.small_exchange_set_keyvault_uri.value
    }
    elseif ( $exchangeSet -eq "medium" ) {
        $exchangeSetWebapps = $terraformOutput.medium_exchange_set_webapps.value
        $keyVaultUri = $terraformOutput.medium_exchange_set_keyvault_uri.value
    }
    elseif ( $exchangeSet -eq "large" ) {
        $exchangeSetWebapps = $terraformOutput.large_exchange_set_webapps.value
        $keyVaultUri = $terraformOutput.large_exchange_set_keyvault_uri.value
    }
    else {
        throw "Invalid exchange set type: $exchangeSet"
    }
    
    Expand-Archive -Path "$packagePath/$packageName" -DestinationPath "$packagePath/$exchangeSet/"
    if ( !$? ) { echo "Error while unzip" ; throw $_ }

    foreach ($webapp in $exchangeSetWebapps) 
    {
        $webappName = $webapp.webappName
        $queueName  = $webapp.queueName
        
        if ( !$? ) { echo "Error while pattern match" ; throw $_ }

        echo "Replacing queue name $queueName for $webappName in $exchangeSet exchange set..."

        $appSettingFileForFulfilment = "$packagePath/$exchangeSet/App_Data/jobs/continuous/ESSFulfilmentWebJob/appsettings.json"
        $appSettingForFulfilment = Get-Content $appSettingFileForFulfilment |ConvertFrom-Json

        if ( !$? ) { echo "Error while Reading json file for fulfilment" ; throw $_ }

        $appSettingForFulfilment.ESSFulfilmentStorageConfiguration.QueueName = $queueName
        $appSettingForFulfilment.KeyVaultSettings.ServiceUri = $keyVaultUri
        $appSettingForFulfilment | ConvertTo-Json -Depth 5 | set-content $appSettingFileForFulfilment
        
        if ( !$? ) { echo "Error while updating json file for fulfilment" ; throw $_ }
        
        echo "Replacing queue name $queueName for $webappName in $exchangeSet exchange set done ..."

        echo "Creating zip package for $webappName in $exchangeSet exchange set ..."
        Compress-Archive -Path "$packagePath/$exchangeSet/*" -DestinationPath "$packagePath/$exchangeSet/$packageName" -Force
        
        if ( !$? ) { echo "Error while zip" ; throw $_ }
        
        echo "Applying staging slot sticky appsettings - $webappName in $exchangeSet"
        az webapp config appsettings set --resource-group $webAppResourceGroup --name $webappName --slot staging --slot-settings WEBJOBS_STOPPED=1

        echo "Creating zip package for $webappName in $exchangeSet exchange set done ..."

        echo "Deploying web app $webappName for $exchangeSet exchange set to staging slot..."
        az webapp deployment source config-zip -g $webAppResourceGroup --slot staging -n $webappName --src "$packagePath/$exchangeSet/$packageName"
        
        if ( !$? ) { echo "Error while deploying package" ; throw $_ }

        echo "Swap production slot of web app $webappName for $exchangeSet exchange set to staging slot..."

        az webapp deployment slot swap --name $webappName --resource-group $webAppResourceGroup --slot staging

        echo "Deploying web app $webappName for $exchangeSet exchange set done ..."
    
        echo "Cleaning up package for next deployment for $exchangeSet exchange set ..."
        Remove-Item "$packagePath/$exchangeSet/$packageName"

        if ( !$? ) { echo "Error while cleaning up temp directory" ; throw $_ }

        echo "Cleaning up package for next deployment for $exchangeSet exchange set done ..."
    }

    echo "$exchangeSet exchange set deployment completed cleaning up ..."
    Remove-Item "$packagePath/$exchangeSet/" -Recurse

    if ( !$? ) { echo "Error while cleaning up exchange set directory" ; throw $_ }

    echo "$exchangeSet exchange set deployment completed cleaning up done ..."
}

$terraformOutput = Get-Content $terraformJsonOutputFile | ConvertFrom-Json
if ( !$? ) { echo "Error while Reading terraform output" ; throw $_ }

echo "Deploying $exchangeSetSize exchange set ..."
ReplaceQueueAndDeployWebApp $packagePath $packageName $exchangeSetSize $terraformOutput.web_app_resource_group.value

if ( !$? ) { echo "Error while replacing queue and deploying $exchangeSetSize exchange set webapps" ; throw $_ }

echo "Deploying $exchangeSetSize exchange set done ..."

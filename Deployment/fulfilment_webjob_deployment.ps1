param (
    [Parameter(Mandatory = $true)] [string] $terraformJsonOutputFile,
    [Parameter(Mandatory = $true)] [string] $packagePath,
    [Parameter(Mandatory = $true)] [string] $packageName
)

echo "terraformJsonOutputFile : $terraformJsonOutputFile"
echo "packagePath : $packagePath"
echo "packageName : $packageName"

function ReplaceQueueAndDeployWebApp($exchangeSetWebapps, $packagePath, $packageName, $exchangeSet, $KeyVaultUri, $webAppResourceGroup){
    
    echo "Function ReplaceQueueAndDeployWebApp called with params $exchangeSetWebapps, $packagePath, $packageName, $exchangeSet, $KeyVaultUri"

    Expand-Archive -Path "$packagePath/$packageName" -DestinationPath "$packagePath/$exchangeSet/"
    if ( !$? ) { echo "Error while unzip" ; throw $_ }

    foreach ($webapp in $exchangeSetWebapps) 
    {
        $webapp -match 'queuename=(?<queueName>.+);\swebappname=(?<webappName>.+)}'
        $webappName = $Matches.webappName
        $queueName  = $Matches.queueName
        
        if ( !$? ) { echo "Error while pattern match" ; throw $_ }

        echo "Replacing queue name $queueName for $webappName in $exchangeSet exchange set..."

        $appSettingFile = "$packagePath/$exchangeSet/App_Data/jobs/continuous/ESSFulfilmentWebJob/appsettings.json"
        $appSetting = Get-Content $appSettingFile |ConvertFrom-Json

        if ( !$? ) { echo "Error while Reading json file" ; throw $_ }

        $appSetting.ESSFulfilmentStorageConfiguration.QueueName = $queueName
        $appSetting.KeyVaultSettings.ServiceUri = $KeyVaultUri
        $appSetting | ConvertTo-Json | set-content $appSettingFile
        
        if ( !$? ) { echo "Error while updating json file" ; throw $_ }
        
        echo "Replacing queue name $queueName for $webappName in $exchangeSet exchange set done ..."

        echo "Creating zip package for $webappName in $exchangeSet exchange set ..."
        Compress-Archive -Path "$packagePath/$exchangeSet/*" -DestinationPath "$packagePath/$exchangeSet/$packageName" -Force
        
        if ( !$? ) { echo "Error while zip" ; throw $_ }
        
        echo "Creating zip package for $webappName in $exchangeSet exchange set done ..."

        echo "Deploying web app $webappName for $exchangeSet exchange set ..."
        az webapp deployment source config-zip -g $webAppResourceGroup -n $webappName --src "$packagePath/$exchangeSet/$packageName"
        
        if ( !$? ) { echo "Error while deploying package" ; throw $_ }

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


echo "Deploying small exchange set ..."
ReplaceQueueAndDeployWebApp $terraformOutput.small_exchange_set_webapps.value $packagePath $packageName "small" $terraformOutput.small_exchange_set_keyvault_uri.value $terraformOutput.web_app_resource_group.value

if ( !$? ) { echo "Error while replacing queue and deploying small exchange set webapps" ; throw $_ }

echo "Deploying small exchange set done ..."

echo "Deploying medium exchange set ..."
ReplaceQueueAndDeployWebApp $terraformOutput.medium_exchange_set_webapps.value $packagePath $packageName "medium" $terraformOutput.medium_exchange_set_keyvault_uri.value $terraformOutput.web_app_resource_group.value

if ( !$? ) { echo "Error while replacing queue and deploying medium exchange set webapps" ; throw $_ }

echo "Deploying medium exchange set done ..."

echo "Deploying large exchange set ..."
ReplaceQueueAndDeployWebApp $terraformOutput.large_exchange_set_webapps.value $packagePath $packageName "large" $terraformOutput.large_exchange_set_keyvault_uri.value $terraformOutput.web_app_resource_group.value

if ( !$? ) { echo "Error while replacing queue and deploying large exchange set webapps" ; throw $_ }

echo "Deploying large exchange set done ..."
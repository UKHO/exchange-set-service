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
    
    foreach ($webapp in $exchangeSetWebapps) 
    {
        $webapp -match 'queuename=(?<queueName>.+);\swebappname=(?<webappName>.+)}'
        $webappName = $Matches.webappName
        $queueName  = $Matches.queueName

        echo "Replacing queue name $queueName for $webappName in $exchangeSet exchange set..."

        $appSettingFile = "$packagePath/$exchangeSet/App_Data/jobs/continuous/ESSFulfilmentWebJob/appsettings.json"
        $appSetting = Get-Content $appSettingFile |ConvertFrom-Json
        $appSetting.ESSFulfilmentStorageConfiguration.QueueName = $queueName
        $appSetting.KeyVaultSettings.ServiceUri = $KeyVaultUri
        $appSetting | ConvertTo-Json | set-content $appSettingFile

        echo "Replacing queue name $queueName for $webappName in $exchangeSet exchange set done ..."

        echo "Creating zip package for $webappName in $exchangeSet exchange set ..."
        Compress-Archive -Path "$packagePath/$exchangeSet/*" -DestinationPath "$packagePath/$exchangeSet/$packageName" -Force
        echo "Creating zip package for $webappName in $exchangeSet exchange set done ..."

        echo "Deploying web app $webappName for $exchangeSet exchange set ..."
        az webapp deployment source config-zip -g $webAppResourceGroup -n $webappName --src "$packagePath/$exchangeSet/$packageName"
        echo "Deploying web app $webappName for $exchangeSet exchange set done ..."
    
        echo "Cleaning up package for next deployment for $exchangeSet exchange set ..."
        Remove-Item –path "$packagePath/$exchangeSet/$packageName"
        echo "Cleaning up package for next deployment for $exchangeSet exchange set done ..."
    }

    echo "$exchangeSet exchange set deployment completed cleaning up ..."
    Remove-Item -Path "$packagePath/$exchangeSet/" -Recurse
    echo "$exchangeSet exchange set deployment completed cleaning up done ..."
}

$terraformOutput = Get-Content $terraformJsonOutputFile | ConvertFrom-Json

echo "Deploying small exchange set ..."
ReplaceQueueAndDeployWebApp $terraformOutput.small_exchange_set_webapps.value $packagePath $packageName "small" $terraformOutput.small_exchange_set_keyvault_uri.value $terraformOutput.web_app_resource_group.value
echo "Deploying small exchange set done ..."
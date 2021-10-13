param (
    [Parameter(Mandatory = $true)] [string] $resourceGroup
)

echo "ResourceGroup : $resourceGroup ..."

$rgExists = az group exists -n $resourceGroup

echo "Resource group $resourceGroup Exists: $rgExists ..."

if ($rgExists) {
    echo "Resource group $resourceGroup exists deleting it ..."
    
    az group delete -y --resource-group $resourceGroup

    echo "Resource group delete completed ..."
}
else
{
    echo "Resource group $resourceGroup doesn't exists returing from script ..."
}


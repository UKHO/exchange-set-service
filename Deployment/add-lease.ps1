Param(
    [Parameter(mandatory=$true)][int]$daysValid,
	[Parameter(mandatory=$true)][string]$accessToken,
    [Parameter(mandatory=$true)][int]$definitionId,
    [Parameter(mandatory=$true)][string]$ownerId,
    [Parameter(mandatory=$true)][int]$buildId,
    [Parameter(mandatory=$true)][string]$collectionUri,
    [Parameter(mandatory=$true)][string]$teamProject    
)

try{
    $contentType = "application/json";
    $headers = @{ Authorization = $accessToken };
    $rawRequest = @{ daysValid = $daysValid; definitionId = $definitionId; ownerId = $ownerId; protectPipeline = $false; runId = $buildId };
    $request = ConvertTo-Json @($rawRequest);
    $uri = "$collectionUri$teamProject/_apis/build/retention/leases?api-version=7.0";

    Write-Host $request
    Write-Host $uri

    Invoke-RestMethod -uri $uri -method POST -Headers $headers -ContentType $contentType -Body $request;

    Write-Host "Pipeline will be retained for $daysValid days"
}
catch{
   Write-Host "##vso[task.LogIssue type=warning;]Pipeline retaintion failed."
   Write-Host $_
   Write-Host "##vso[task.complete result=SucceededWithIssues;]"
}

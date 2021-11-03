param($queueSecret,$pollSecret,$pollTimeInSecs,$functionEndPoint)

$queueBuildEndPoint = "${functionEndPoint}/api/queue-e2e-test?code=${queueSecret}"
$pollBuildEndPoint = "${functionEndPoint}/api/poll-e2e-test?code=${pollSecret}&runId="

function Execute-GetAgainstEndpoint {
    param (
        $endPoint
    )

    $response

    try {
        $response = Invoke-RestMethod -Method Get -Uri $endPoint -UseBasicParsing
    } catch {
        Write-Host "`nStatusCode:" $_.Exception.Response.StatusCode.value__ 
        Write-Host "StatusDescription:" $_.Exception.Response.StatusDescription
    }

    if ($response.statusCode -ne 200) {
        throw "Function call to ${endPoint} returned ${response}"
    }

    return $response
}

$queueBuildResponse = Execute-GetAgainstEndpoint($queueBuildEndPoint)

$buildId = $queueBuildResponse.responseBody.id

Write-Output "`nADDS E2E build ${buildId} has been queued. Beginning polling..."

$pollBuildResponse

do
{
    Write-Output "Waiting for build status to be completed..."
    start-sleep $pollTimeInSecs
 
    $pollBuildResponse = Execute-GetAgainstEndpoint("${pollBuildEndPoint}${buildId}")

} while ($pollBuildResponse.responseBody.state -ne "completed")

$buildResult = $pollBuildResponse.responseBody.result
$buildUrl = $pollBuildResponse.responseBody._links.web.href

Write-Output "`nBuild completed verifying result..."

if($buildResult -eq "succeeded")
{
    Write-Output "`nBuild ${buildId} has succeeded."
    Write-Output "`n${buildUrl}"
}
else
{
    throw "The result for build ${buildId} is ${buildResult}, please review build: ${buildUrl}"
}
Param(
	[Parameter(mandatory=$true)][string]$healthEndPointUrl,
    [Parameter(mandatory=$true)][string]$waitTimeInMinute
)

$sleepTimeInSecond = 15
$isServiceActive = 'false'

$stopWatch = New-Object -TypeName System.Diagnostics.Stopwatch
$timeSpan = New-TimeSpan -Minutes $waitTimeInMinute
$stopWatch.Start()

do
{
    Write-Host "Polling url: $healthEndPointUrl ..."
    try{
        Start-Sleep -Seconds $sleepTimeInSecond
        $HttpRequest  = [System.Net.WebRequest]::Create("$healthEndPointUrl")
        $HttpResponse = $HttpRequest.GetResponse() 
        $HttpStatus   = $HttpResponse.StatusCode
        Write-Host "Status code of web is $HttpStatus ..."
    
        If ($HttpStatus -eq 200 ) {
            Write-Host "Service is up. Stopping Polling ..."
            $isServiceActive = 'true'
            break
        }
        Else {
            Write-Host "Service not yet Up. Status code: $HttpStatus ..."
            Start-Sleep -Seconds $sleepTimeInSecond
        }
    }
    catch [System.Net.WebException]
    {
        $HttpStatus = $_.Exception.Response.StatusCode
        Write-Host "Service not yet Up.Status: $HttpStatus ..."
        Start-Sleep -Seconds $sleepTimeInSecond
    }
    
    
}
until ($stopWatch.Elapsed -ge $timeSpan)


If ($HttpResponse -ne $null) { 
    $HttpResponse.Close() 
}

if ($isServiceActive -eq 'true' ) {
    Write-Host "Service is up returning from script ..."
}
Else { 
    Write-Error "Service was not up in $waitTimeInMinute, error while deployment ..."
    throw "Error"
}
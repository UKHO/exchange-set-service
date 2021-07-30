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
            Write-Host "Service not yet Up. Status code: $HttpStatus re-checking after $sleepTimeInSecond sec ..."
        }
    }
    catch [System.Net.WebException]
    {
        $HttpStatus = $_.Exception.Response.StatusCode
        Write-Host "Service not yet Up.Status: $HttpStatus re-checking after $sleepTimeInSecond sec ..."
    }    
    
    Start-Sleep -Seconds $sleepTimeInSecond
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
param (
    [Parameter(Mandatory = $true)] [string] $reportFolder,
    [Parameter(Mandatory = $true)] [string] $sourceFolder
)

try {
    reportgenerator "-reports:$sourceFolder/**/coverage.cobertura.xml" "-targetdir:$reportFolder\codecoveragereport" "-reporttypes:HtmlInline_AzurePipelines;Cobertura";
    write-host 'CoverageReport Published - '  $reportFolder\codecoveragereport;

}
catch [System.Management.Automation.CommandNotFoundException] {
    write-host "report generator not present installing and re-attempting to generate report.";
    dotnet tool install -g dotnet-reportgenerator-globaltool;

    reportgenerator "-reports:$sourceFolder/**/coverage.cobertura.xml" "-targetdir:$reportFolder\codecoveragereport" "-reporttypes:HtmlInline_AzurePipelines;Cobertura";
    write-host 'CoverageReport Published - '  $reportFolder\codecoveragereport;

}
catch {

    write-host "Caught an exception:"; 
    write-host "Exception Type: $($_.Exception.GetType().FullName)";
    write-host "Exception Message: $($_.Exception.Message)"; 
}
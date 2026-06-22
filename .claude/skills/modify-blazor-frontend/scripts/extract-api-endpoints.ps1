#Requires -Version 5.1
<#
.SYNOPSIS
    Extract API endpoints from Paraki.API controllers.

.DESCRIPTION
    Scans controller files and extracts HTTP endpoints with their attributes,
    parameters, and documentation. Output can be used to verify the API contract.

.PARAMETER ApiPath
    Path to the API project root. Default: current directory.

.EXAMPLE
    .\extract-api-endpoints.ps1 -ApiPath "C:\repo\src\Paraki.API"
#>

param(
    [string]$ApiPath = "."
)

$controllerPath = Join-Path $ApiPath "Controllers"

if (-not (Test-Path $controllerPath)) {
    Write-Error "Controllers directory not found at $controllerPath"
    exit 1
}

$endpoints = @()

Get-ChildItem $controllerPath -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw

    # Extract class name and route
    $classMatch = [regex]::Match($content, '\[Route\("(?<route>[^"]+)"\)\].*?public class (?<class>\w+)')
    if (-not $classMatch.Success) { return }

    $classRoute = $classMatch.Groups['route'].Value
    $className = $classMatch.Groups['class'].Value

    # Extract methods with HTTP attributes
    $methodPattern = '\[(Http(?:Get|Post|Put|Patch|Delete))\("?(?<route>[^"]*)"?\)?\]\s*(?:\[.*?\]\s*)*public\s+(?:async\s+)?Task(?:<\w+>)?\s+(?<method>\w+)'

    $matches = [regex]::Matches($content, $methodPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    foreach ($match in $matches) {
        $httpMethod = $match.Groups[1].Value
        $route = $match.Groups['route'].Value
        $methodName = $match.Groups['method'].Value

        # Build full route
        $fullRoute = "$classRoute/$route" -replace '\/+', '/'
        $fullRoute = $fullRoute.TrimEnd('/')

        $endpoints += @{
            Method = $httpMethod
            Route = $fullRoute
            Controller = $className
            Action = $methodName
        }
    }
}

# Output as markdown
$endpoints | Group-Object Controller | ForEach-Object {
    Write-Host "`n## $($_.Name)"

    $_.Group | ForEach-Object {
        $color = switch ($_.Method) {
            'HttpGet'    { 'Green' }
            'HttpPost'   { 'Cyan' }
            'HttpPut'    { 'Yellow' }
            'HttpDelete' { 'Red' }
            default      { 'White' }
        }

        Write-Host "  [$($_.Method -replace 'Http')] $($_.Route)" -ForegroundColor $color
    }
}

Write-Host "`nTotal endpoints found: $($endpoints.Count)" -ForegroundColor Cyan

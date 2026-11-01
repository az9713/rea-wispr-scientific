param([switch]$Publish)
. (Join-Path $PSScriptRoot 'bootstrap.ps1')
Set-Location $projectRoot
& $env:SCIENTIFIC_DOTNET build src\Scientific.App\Scientific.App.csproj -c Release --nologo
if($LASTEXITCODE){throw 'App build failed'}
& $env:SCIENTIFIC_DOTNET build src\Scientific.Cli\Scientific.Cli.csproj -c Release --nologo
if($LASTEXITCODE){throw 'CLI build failed'}
if($Publish){
 $destination=Join-Path $projectRoot 'releases\Scientific-Dictation-win-x64'
 & $env:SCIENTIFIC_DOTNET publish src\Scientific.App\Scientific.App.csproj -c Release -r win-x64 --self-contained true -o $destination --nologo
 if($LASTEXITCODE){throw 'App publish failed'}
 & $env:SCIENTIFIC_DOTNET publish src\Scientific.Cli\Scientific.Cli.csproj -c Release -r win-x64 --self-contained true -o $destination --nologo
 if($LASTEXITCODE){throw 'CLI publish failed'}
 Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $destination 'LICENSE.txt')
 Copy-Item -LiteralPath (Join-Path $projectRoot 'NOTICE.md') -Destination (Join-Path $destination 'SCIENTIFIC-NOTICE.md')
 Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses\DOTNET-LICENSE.txt') -Destination (Join-Path $destination 'DOTNET-LICENSE.txt')
 Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $destination 'README.md')
 Copy-Item -LiteralPath (Join-Path $projectRoot 'fixtures\parser-cases.json') -Destination (Join-Path $destination 'parser-cases.json')
}

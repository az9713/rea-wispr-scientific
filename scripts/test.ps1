. (Join-Path $PSScriptRoot 'bootstrap.ps1')
Set-Location $projectRoot
$output=Join-Path $projectRoot ('fixtures\run-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $output | Out-Null
& $env:SCIENTIFIC_DOTNET run --project src\Scientific.Cli\Scientific.Cli.csproj -c Release -- --self-test (Join-Path $output 'core')
if($LASTEXITCODE){throw 'Core tests failed'}
& $env:SCIENTIFIC_DOTNET run --project src\Scientific.Cli\Scientific.Cli.csproj -c Release -- --fixtures (Join-Path $projectRoot 'fixtures\parser-cases.json')
if($LASTEXITCODE){throw 'Fixture tests failed'}
& $env:SCIENTIFIC_DOTNET (Join-Path $projectRoot 'src\Scientific.App\bin\Release\net9.0-windows\scientific.dll') --self-test (Join-Path $output 'ui')
if($LASTEXITCODE){throw 'UI self-test failed'}
python (Join-Path $PSScriptRoot 'test-codegen.py')
if($LASTEXITCODE){throw 'Generated C# checks failed'}
Write-Output "Verification outputs: $output"

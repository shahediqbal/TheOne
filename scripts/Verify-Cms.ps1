$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK is required.' }
if ([string]::IsNullOrWhiteSpace($env:THEONE_AUTH_TEST_CONNECTION)) {
    throw 'Set THEONE_AUTH_TEST_CONNECTION to a disposable PostgreSQL test database. Tests apply migrations.'
}
# Fail closed if a production-looking database name was accidentally supplied.
if ($env:THEONE_AUTH_TEST_CONNECTION -notmatch '(?i)(?:^|;)\s*Database\s*=\s*[^;]*_tests?\s*(?:;|$)') {
    throw 'For this helper, the disposable database name must end in _test or _tests.'
}
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
dotnet restore src/TheOne.IntegrationTests/TheOne.IntegrationTests.csproj
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
dotnet build src/TheOne.IntegrationTests/TheOne.IntegrationTests.csproj --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
dotnet test src/TheOne.IntegrationTests/TheOne.IntegrationTests.csproj --no-build --filter 'FullyQualifiedName~WebsiteTests|FullyQualifiedName~BlogTests|FullyQualifiedName~Membership|FullyQualifiedName~BrowserAuthTests' --logger 'trx;LogFileName=cms-membership.trx'
if ($LASTEXITCODE -ne 0) { throw 'CMS/membership regression tests failed.' }
Write-Host 'CMS/membership build and PostgreSQL regression tests passed.'

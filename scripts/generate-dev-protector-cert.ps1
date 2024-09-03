$password = Read-Host -Prompt "Enter password for certificate" -AsSecureString

$outputPathDefault = "$env:USERPROFILE\.nellebot-certs\dev-protector.pfx"

$outputPath = Read-Host -Prompt "Enter certificate output path [$outputPathDefault]"

if ( [string]::IsNullOrWhiteSpace($outputPath))
{
    $outputPath = $outputPathDefault
}

if (Test-Path $outputPath)
{
    Write-Error "Certificate output path already exists: $outputPath"
    exit 1
}

# Create output directory if it doesn't exist
$null = New-Item -ItemType Directory -Force -Path (Split-Path $outputPath)

$cert = New-SelfSignedCertificate `
-KeyUsage KeyEncipherment `
-KeyAlgorithm RSA `
-KeyLength 2048 `
-Type Custom `
-NotBefore (Get-Date) `
-NotAfter ((Get-Date).AddYears(10)) `
-Subject "CN=DEV_PROTECTOR_CERT" `
-CertStoreLocation "Cert:\CurrentUser\My"

Export-PfxCertificate `
-Cert "Cert:\CurrentUser\My\$( $cert.Thumbprint )" `
-FilePath $outputPath `
-Password $password

Write-Host "Exported certificate with thumbprint: $( $cert.Thumbprint )"

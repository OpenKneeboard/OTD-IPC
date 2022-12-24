<#

.SYNOPSIS

    Extract the build artifacts for installation or upload

.PARAMETER Configuration

    The current build configuration (Debug or Release)

.PARAMETER Out

    Directory to copy the artifacts to

.EXAMPLE

  ./copy-artifacts -Configuration Debug 

.EXAMPLE

  ./copy-artifacts -Configuration Debug -Out ../out/

#>

[cmdletbinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string] $Configuration='Debug',
  [string] $Out="$(Get-Location)",
  [string] $Platform="x64"
)

If (!(Test-Path $Out)) {
  New-Item -ItemType Directory -Path $Out
}

$compress = @{
  Path = "OTDIPC/bin/${Configuration}/net6.0/OpenKneeboard-OTDIPC.dll", "OTDIPC/metadata.json"
  DestinationPath = "${Out}\OpenKneeboard-OTD-IPC.zip"
  Force = $True
}
Compress-Archive @compress

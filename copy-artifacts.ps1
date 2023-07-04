<#

.SYNOPSIS

    Extract the build artifacts for installation or upload

.PARAMETER Configuration

    The current build configuration (Debug or Release)

.PARAMETER Out

    Directory to copy the artifacts to

.PARAMETER BuildNumber

    The 'd' in 'a.b.c.d'

.EXAMPLE

  ./copy-artifacts -Configuration Debug 

.EXAMPLE

  ./copy-artifacts -Configuration Debug -Out ../out/

#>

[cmdletbinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string] $Configuration='Debug',
  [string] $Out="$(Get-Location)/out",
  [string] $Platform="x64",
  [int] $BuildNumber=0
)

If (!(Test-Path $Out)) {
  New-Item -ItemType Directory -Path $Out
}

$pluginVersion = "1.0.2.${BuildNumber}"
Set-Content $Out/version.txt -Value $pluginVersion

$metadataJson = "${Out}/metadata.json"

(Get-Content -Path OTDIPC/metadata.in.json) `
  -Replace "@PLUGIN_VERSION@","${pluginVersion}" `
  | Set-Content -Path $metadataJson

$compress = @{
  Path = @(
    "OTDIPC/bin/${Configuration}/net6.0/OpenKneeboard-OTDIPC.dll",
    $metadataJson
  )
  DestinationPath = "${Out}\OpenKneeboard-OTD-IPC.zip"
  Force = $True
}
Compress-Archive @compress
Remove-Item $metadataJson

Copy-Item OTDIPC/bin/$Configuration/net6.0/*.pdb $Out
Copy-Item $Platform/$Configuration/*.pdb $Out
Copy-Item $Platform/$Configuration/*.exe $Out

$compress = @{
  Path = (Get-ChildItem -Path $Out -Filter '*.pdb')
  DestinationPath = "${Out}/DebugSymbols.zip"
  Force = $True
}
Compress-Archive @compress

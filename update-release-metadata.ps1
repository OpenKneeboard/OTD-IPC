<#

.SYNOPSIS

    Create the metadata JSON for a release in a checkout of the OpenTabletDriver/Plugin-Repository repo.

.PARAMETER PluginRepositoryPath

    Path to a checkout of OpenTabletDriver/Plugin-Repository

.EXAMPLE

  In A GitHub Actions workflow:

  ./update-release-metadata.ps1 -GitHubToken ${{github.token}} -PluginRepositoryPath ..\Plugin-Repository

#>

[cmdletbinding()]
param(
  [string] $GitHubToken="",
  [string] $PluginRepositoryPath
)

$Repo = "OpenKneeboard/OTD-IPC"
$AssetPrefix = "OpenKneeboard-OTD-IPC"
$FilePattern = "${AssetPrefix}*.zip"
$OutputName = "$($Repo.Split('/')[-1]).json"

$Release = (gh release view --json assets) | ConvertFrom-Json

$Asset = $Release.assets
  | Where-Object -FilterScript { $_.name -like $FilePattern }

$DownloadPath = Join-Path $env:TEMP "OTD-IPC-Update-$($Asset.name)" -Force
$DownloadUrl = $Asset.url

if (-not (Test-Path -Path "$DownloadPath")) {
  gh release download -O "$DownloadPath" -p "$FilePattern"
}

$BaseName = (Get-Item $DownloadPath).BaseName
$Extracted = Join-Path $env:TEMP $BaseName
if (Test-Path -Path $Extracted) {
  Remove-Item -Path $Extracted -Recurse -Force
}
Expand-Archive -Path $DownloadPath -DestinationPath $Extracted

$Metadata = Get-Content (Join-Path $Extracted "metadata.json")
  | ConvertFrom-Json -AsHashtable
$Metadata += @{
  "DownloadUrl" = $DownloadUrl;
  "CompressionFormat" = "zip";
  "SHA256" = (Get-FileHash -Algorithm SHA256 -Path $DownloadPath).Hash.ToLower()
}

Write-Output "Release Metadata:"
Write-Output $Metadata

if ("$PluginRepositoryPath" -eq "") {
  echo "`nNo OTD PluginRepository path specified, nothing to do."
  return
}

$OutputDirectory = Join-Path `
  (Get-Item $PluginRepositoryPath).FullName `
  "Repository" `
  $Metadata.SupportedDriverVersion `
  $Repo
$OutputPath = Join-Path $OutputDirectory $OutputName

Write-Output "`nOutput path: ${OutputPath}"

if (-not (Test-Path $OutputDirectory)) {
  New-Item -Path $OutputDirectory -ItemType Directory | Out-Null
}

$Metadata
  | ConvertTo-Json -Depth 8
  | % { ($_ -replace "`r`n","`n") + "`n" } # unix newlines with newline at EOF
  | Out-File -Encoding utf8NoBOM -FilePath $OutputPath -NoNewline # no auto CRLF

echo "`nUpdated metadata in $OutputPath`n`nNext, commit and create a pull request!"

<#

.SYNOPSIS

    Create the metadata JSON for a release in a checkout of the OpenTabletDriver/Plugin-Repository repo.

.PARAMETER GitHubToken

    GitHub API token

.PARAMETER PluginRepositoryPath

    Path to a checkout of OpenTabletDriver/Plugin-Repository

.EXAMPLE

  In A GitHub Actions workflow:

  ./update-release-metadata.ps1 -GitHubToken ${{GITHUB_TOKEN}} -PluginRepositoryPath ..\Plugin-Repository

#>

[cmdletbinding()]
param(
  [string] $GitHubToken="",
  [string] $PluginRepositoryPath
)

$Repo = "OpenKneeboard/OTD-IPC"
$AssetPrefix = "OpenKneeboard-OTD-IPC-"
$OutputName = "$($Repo.Split('/')[-1]).json"

$DownloadHeaders = @{
  "User-Agent" = "${Repo} - $((Get-Item $PSCommandPath).Name) v0.0.0-filehash.$((Get-FileHash $PSCommandPath).Hash)";
}
$ApiHeaders = $DownloadHeaders + @{
  "Accept" = "application/vnd.github+json";
  "X-GitHub-Api-Version" = "2022-11-28";
}
if ("${GitHubToken}" -ne "") {
  $ApiHeaders['Authorization'] = $GitHubToken
  $DownloadHeaders['Authorization'] = $GitHubToken
}

$Release = (Invoke-WebRequest -Uri https://api.github.com/repos/${Repo}/releases -Headers $ApiHeaders).Content
  | ConvertFrom-Json
  | Where-Object -Not -Property 'prerelease'
  | Sort-Object -Descending -Property { [System.Management.Automation.SemanticVersion] ($_.tag_name -replace '^v' -replace 'beta','beta.') }
  | Select-Object -First 1

$FilePattern = "${AssetPrefix}$($Release.tag_name)*.zip"

$Asset = $Release.assets
  | Where-Object -FilterScript { $_.name -like $FilePattern }

$DownloadUrl = $Asset.browser_download_url

$DownloadPath = Join-Path $env:TEMP $Asset.name

Invoke-WebRequest `
  -Uri $DownloadUrl `
  -Headers $DownloadHeaders `
  -OutFile $DownloadPath

$BaseName = (Get-Item $DownloadPath).BaseName
$Extracted = Join-Path $env:TEMP $BaseName
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
  | Out-File -Encoding utf8NoBOM -FilePath $OutputPath
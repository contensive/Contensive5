<#
.SYNOPSIS
    Contensive shared module for uploading collections to the Addon Collection Library.
    Host: C:\Git\Contensive5\scripts\deploy-to-addon-library.psm1

.DESCRIPTION
    Provides Invoke-AddonLibraryDeploy — the single entry point that parses
    a collection XML for GUID and description, locates the built zip and
    optional promo image, then uploads everything to the addon-library-upload
    remote method on the target Contensive site.

    Typical caller (scripts/deploy-to-addon-library.ps1 in an addon repo):

        Import-Module (Join-Path $PSScriptRoot '..\..\Contensive5\scripts\deploy-to-addon-library.psm1') -Force

        Invoke-AddonLibraryDeploy `
            -CollectionName 'My Addon' `
            -CollectionPath "$projectRoot\collections\MyAddon" `
            -DeploymentPath 'C:\Deployments\aoMyAddon' `
            -UiPath         "$projectRoot\ui"
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ===========================================================================
# Invoke-AddonLibraryDeploy
# ===========================================================================

function Invoke-AddonLibraryDeploy {
<#
.SYNOPSIS
    Uploads a built collection to the Addon Collection Library on contensive.com.

.DESCRIPTION
    Parses the collection XML for GUID and description (Help node), finds the
    collection zip and optional promo image, validates that required library
    listing content is present, then POSTs everything to the addon-library-upload
    remote method on the target Contensive site.

.PARAMETER CollectionName
    Display name of the collection, e.g. "My Addon".

.PARAMETER CollectionPath
    Path to the folder containing the collection XML file.

.PARAMETER DeploymentPath
    Path where the built collection zip lives.

.PARAMETER UiPath
    Optional path to the UI folder. The function looks for a promo image
    in the libraryFiles subfolder.

.PARAMETER TargetDomain
    Domain of the Contensive site hosting the Addon Collection Library.
    Defaults to "contensive.com".
#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CollectionName,
        [Parameter(Mandatory)][string]$CollectionPath,
        [Parameter(Mandatory)][string]$DeploymentPath,
        [string]$UiPath = '',
        [string]$TargetDomain = 'contensive.com'
    )

    Write-Host ""
    Write-Host "========================================"
    Write-Host "  Deploy to Addon Collection Library"
    Write-Host "  Collection:  $CollectionName"
    Write-Host "  Target:      $TargetDomain"
    Write-Host "========================================"
    Write-Host ""

    # -------------------------------------------------------------------
    # Step 1: Find and parse the collection XML
    # -------------------------------------------------------------------
    if (-not (Test-Path $CollectionPath)) {
        throw "Collection path not found: $CollectionPath"
    }
    $xmlFiles = Get-ChildItem -Path $CollectionPath -Filter '*.xml' -File
    if ($xmlFiles.Count -eq 0) {
        throw "No XML file found in: $CollectionPath"
    }
    $xmlFile = $xmlFiles[0]
    Write-Host "Collection XML: $($xmlFile.FullName)"

    [xml]$collectionXml = Get-Content $xmlFile.FullName -Encoding UTF8
    $collectionGuid = $collectionXml.Collection.Guid
    $helpText = $collectionXml.Collection.Help

    if ([string]::IsNullOrWhiteSpace($collectionGuid)) {
        throw "No Guid attribute found on <Collection> element in $($xmlFile.Name)"
    }

    Write-Host "GUID:          $collectionGuid"
    if ($helpText) {
        $preview = if ($helpText.Length -gt 80) { $helpText.Substring(0, 80) + '...' } else { $helpText }
        Write-Host "Description:   $preview"
    } else {
        Write-Host "Description:   (empty)"
    }

    # -------------------------------------------------------------------
    # Step 2: Find promo image in ui/{project}/libraryFiles/
    # -------------------------------------------------------------------
    $promoImageBase64 = ''
    $promoImageFileName = ''
    if ($UiPath -and (Test-Path $UiPath)) {
        $libraryFilesPath = Join-Path $UiPath 'libraryFiles'
        if (Test-Path $libraryFilesPath) {
            $imageFiles = Get-ChildItem -Path $libraryFilesPath -Include '*.png','*.jpg','*.jpeg','*.gif' -File -Recurse
            if ($imageFiles.Count -gt 0) {
                $imageFile = $imageFiles[0]
                $promoImageBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($imageFile.FullName))
                $promoImageFileName = $imageFile.Name
                Write-Host "Promo image:   $promoImageFileName ($($imageFile.Length) bytes)"
            }
        }
    }
    if (-not $promoImageFileName) {
        Write-Host "Promo image:   (none found)"
    }

    # -------------------------------------------------------------------
    # Step 2b: Validate required library listing content
    # -------------------------------------------------------------------
    $missingItems = @()
    if ([string]::IsNullOrWhiteSpace($helpText)) {
        $missingItems += "- <Help> node in $($xmlFile.Name) is empty. Add a description to the <Help> element in the collection XML."
    }
    if (-not $promoImageFileName) {
        $libraryTarget = if ($UiPath) { Join-Path $UiPath 'libraryFiles' } else { 'ui/<project>/libraryFiles' }
        $missingItems += "- No promo image found. Add a display image (png/jpg/gif) to $libraryTarget."
    }
    if ($missingItems.Count -gt 0) {
        Write-Host ""
        Write-Host "========================================"  -ForegroundColor Yellow
        Write-Host "  Missing library listing content:"      -ForegroundColor Yellow
        Write-Host "========================================"  -ForegroundColor Yellow
        foreach ($item in $missingItems) {
            Write-Host $item -ForegroundColor Yellow
        }
        Write-Host ""
        Write-Host "The addon will appear in the library without a description and/or image."
        Write-Host "Press any key to continue anyway, or Ctrl+C to cancel and add the missing content first..."
        $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
        Write-Host ""
    }

    # -------------------------------------------------------------------
    # Step 3: Find the collection zip
    # -------------------------------------------------------------------
    if (-not (Test-Path $DeploymentPath)) {
        throw "Deployment path not found: $DeploymentPath"
    }

    $zipFile = $null

    # First look for .zip files directly in DeploymentPath
    $directZips = Get-ChildItem -Path $DeploymentPath -Filter '*.zip' -File -ErrorAction SilentlyContinue
    if ($directZips.Count -gt 0) {
        $zipFile = $directZips | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    } else {
        # Look in the most recent subfolder (version subfolders sorted descending)
        $subfolders = Get-ChildItem -Path $DeploymentPath -Directory | Sort-Object Name -Descending
        foreach ($sf in $subfolders) {
            $sfZips = Get-ChildItem -Path $sf.FullName -Filter '*.zip' -File -ErrorAction SilentlyContinue
            if ($sfZips.Count -gt 0) {
                $zipFile = $sfZips | Sort-Object LastWriteTime -Descending | Select-Object -First 1
                break
            }
        }
    }
    if (-not $zipFile) {
        throw "No collection zip found in: $DeploymentPath"
    }
    Write-Host "Collection zip: $($zipFile.FullName) ($($zipFile.Length) bytes)"

    # -------------------------------------------------------------------
    # Step 4: Bearer token (cached per domain)
    # -------------------------------------------------------------------
    $tokenDir = Join-Path $env:LOCALAPPDATA 'contensive'
    if (-not (Test-Path $tokenDir)) { New-Item -ItemType Directory -Path $tokenDir -Force | Out-Null }

    $siteName = ($TargetDomain -split '\.')[0]
    $tokenFile = Join-Path $tokenDir "$siteName-library-token.txt"

    $token = ''
    if (Test-Path $tokenFile) {
        $token = (Get-Content $tokenFile -Raw).Trim()
        if ($token) {
            Write-Host "Using cached bearer token for $TargetDomain"
        }
    }
    if (-not $token) {
        $token = Read-Host "Enter bearer token for $TargetDomain"
        if ([string]::IsNullOrWhiteSpace($token)) { throw "No token provided." }
        Set-Content -Path $tokenFile -Value $token -NoNewline
    }

    # -------------------------------------------------------------------
    # Step 5: Base64-encode the collection zip
    # -------------------------------------------------------------------
    Write-Host ""
    Write-Host "Encoding collection zip..."
    $zipBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($zipFile.FullName))
    Write-Host "Encoded size: $([Math]::Round($zipBase64.Length / 1MB, 1)) MB"

    # -------------------------------------------------------------------
    # Step 6: Build JSON payload
    # -------------------------------------------------------------------
    $payload = @{
        collectionGuid       = $collectionGuid
        collectionName       = $CollectionName
        description          = if ($helpText) { $helpText } else { '' }
        collectionFileBase64 = $zipBase64
        collectionFileName   = $zipFile.Name
    }
    if ($promoImageBase64) {
        $payload['promoImageBase64']   = $promoImageBase64
        $payload['promoImageFileName'] = $promoImageFileName
    }

    $jsonPayload = $payload | ConvertTo-Json -Depth 3 -Compress

    # -------------------------------------------------------------------
    # Step 7: POST to the remote method
    # -------------------------------------------------------------------
    $url = "https://$TargetDomain/addon-library-upload"
    Write-Host "Uploading to $url..."
    Write-Host ""

    try {
        $response = Invoke-LibraryUploadRequest -BearerToken $token -Url $url -Body $jsonPayload
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401 -or $statusCode -eq 403) {
            Write-Host "Token rejected (HTTP $statusCode). Please provide a new token."
            $token = Read-Host "Enter new bearer token for $TargetDomain"
            if ([string]::IsNullOrWhiteSpace($token)) { throw "No token provided." }
            Set-Content -Path $tokenFile -Value $token -NoNewline
            $response = Invoke-LibraryUploadRequest -BearerToken $token -Url $url -Body $jsonPayload
        } else {
            throw
        }
    }

    # -------------------------------------------------------------------
    # Step 8: Display result
    # -------------------------------------------------------------------
    if ($response.success) {
        Write-Host "SUCCESS: $($response.message)"
        Write-Host "Record ID: $($response.recordId)"
    } else {
        Write-Host "FAILED: $($response.error)" -ForegroundColor Red
        throw "Addon library upload failed: $($response.error)"
    }

    Write-Host ""
    Write-Host "========================================"
    Write-Host "  Upload complete: $CollectionName -> $TargetDomain"
    Write-Host "========================================"
}

# ===========================================================================
# Private helpers
# ===========================================================================

function Invoke-LibraryUploadRequest {
    param([string]$BearerToken, [string]$Url, [string]$Body)
    $headers = @{
        'Authorization' = "Bearer $BearerToken"
        'Content-Type'  = 'application/json'
    }
    return Invoke-RestMethod -Uri $Url -Method Post -Headers $headers -Body $Body -TimeoutSec 120
}

# ===========================================================================
# Exports
# ===========================================================================

Export-ModuleMember -Function Invoke-AddonLibraryDeploy

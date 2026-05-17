#Requires -Version 5.1
<#
.SYNOPSIS
    aoMcp collection build — configuration only.
    All build steps are defined in the shared Contensive build library.
    Entry point: build.cmd
.PARAMETER Configuration
    Build configuration (Debug or Release). Defaults to Debug.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '..\..\..\scripts\contensive-build.psm1') -Force

$projectRoot = (Resolve-Path "$PSScriptRoot\..").Path

Invoke-ContensiveBuild `
    -CollectionName    'aoMcp' `
    -CollectionPath    "$projectRoot\collections" `
    -SolutionPath      "$projectRoot\server\aoMcp.sln" `
    -BinPath           "$projectRoot\server\bin\$Configuration\netstandard2.0" `
    -DeploymentRoot    'C:\Deployments\aoMcp' `
    -Configuration     $Configuration `
    -CleanFolders      @(
                           "$projectRoot\server\bin"
                           "$projectRoot\server\obj"
                       ) `
    -UiPath            "$projectRoot\ui"

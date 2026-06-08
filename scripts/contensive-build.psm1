<#
.SYNOPSIS
    DEPRECATED — This module has been renamed to build-addon-collection.psm1.

.DESCRIPTION
    This file exists only to notify callers that the shared build library has
    been renamed. Update your build.ps1 to import build-addon-collection.psm1
    instead of contensive-build.psm1.
#>

Write-Warning @"
================================================================================
DEPRECATED: contensive-build.psm1 has been renamed to build-addon-collection.psm1

Please update your build script (scripts/build.ps1) to change:

    Import-Module "...\contensive-build.psm1" -Force

to:

    Import-Module "...\build-addon-collection.psm1" -Force

================================================================================
"@

throw "Build aborted. Update your build script to reference build-addon-collection.psm1 instead of contensive-build.psm1."

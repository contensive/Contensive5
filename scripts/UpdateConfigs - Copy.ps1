# Define config + DLL path pairs
$configMappings = @(
    @{ ConfigPath = "C:\Git\Contensive5\source\Cli\bin\Debug\net472\cc.exe.config"; DllPath = "C:\Git\Contensive5\source\Cli\bin\Debug\net472" },
    @{ ConfigPath = "C:\Git\Contensive5\source\TaskService\bin\Debug\TaskService.exe.config"; DllPath = "C:\Git\Contensive5\source\TaskService\bin\Debug" },
    @{ ConfigPath = "C:\Git\Contensive5\source\iisDefaultSite\web.config"; DllPath = "C:\Git\Contensive5\source\iisDefaultSite\bin" }
)

# Function to get DLL version map from a folder
function Get-DllVersions {
    param ([string]$dllDir)

    $dllVersions = @{}
    $dllFiles = Get-ChildItem -Path $dllDir -Filter *.dll -File

    foreach ($dll in $dllFiles) {
        try {
			# Load the assembly
			# $assembly = [System.Reflection.Assembly]::LoadFrom($dll)

			# Get the assembly version
			# $dllVersions[$assemblyName] =  $assembly.GetName().Version.ToString()
			
			
            $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll.FullName)
            $assemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($dll.FullName).Name
            $dllVersions[$assemblyName] = $versionInfo.FileVersion
        } catch {
            Write-Warning "Failed to read version info for $($dll.Name)"
        }
    }

    return $dllVersions
}

# Function to update a single config file
function Update-AppConfig {
    param (
        [string]$configPath,
        [hashtable]$dllVersions
    )

    [xml]$configXml = Get-Content $configPath
    $nsMgr = New-Object System.Xml.XmlNamespaceManager($configXml.NameTable)
    $nsMgr.AddNamespace("asm", "urn:schemas-microsoft-com:asm.v1")

    foreach ($assemblyName in $dllVersions.Keys) {
        $newVersion = $dllVersions[$assemblyName]
        $xpath = "//asm:dependentAssembly[asm:assemblyIdentity[@name='$assemblyName']]"
        $depAssembly = $configXml.SelectSingleNode($xpath, $nsMgr)

        if ($depAssembly -ne $null) {
            $bindingRedirect = $depAssembly.bindingRedirect
            if ($bindingRedirect -ne $null) {
                $bindingRedirect.oldVersion = "0.0.0.0-$newVersion"
                $bindingRedirect.newVersion = $newVersion
                Write-Host "[$configPath] Updated $assemblyName to $newVersion"
            } else {
                Write-Warning "[$configPath] No bindingRedirect for $assemblyName"
            }
        } else {
            Write-Warning "[$configPath] No dependentAssembly entry for $assemblyName"
        }
    }

    $configXml.Save($configPath)
}

# Loop through each config + DLL path pair
foreach ($mapping in $configMappings) {
    $dllVersions = Get-DllVersions -dllDir $mapping.DllPath
    Update-AppConfig -configPath $mapping.ConfigPath -dllVersions $dllVersions
}

Write-Host "`n✅ All config files updated with their respective DLL versions."
[xml]$x = Get-Content 'c:\Git\Contensive5\source\Processor\aoBase51.xml'
$cdefs = $x.Collection.CDef
foreach ($cdef in $cdefs) {
    $cdefName = $cdef.Name
    $tableName = $cdef.ContentTableName
    $fields = @()
    foreach ($f in $cdef.Field) {
        if ($f -and $f.HelpDefault) {
            $helpText = ""
            if ($f.HelpDefault -is [string]) {
                $helpText = $f.HelpDefault.Trim()
            } elseif ($f.HelpDefault.InnerText) {
                $helpText = $f.HelpDefault.InnerText.Trim()
            } elseif ($f.HelpDefault.'#cdata-section') {
                $helpText = $f.HelpDefault.'#cdata-section'.Trim()
            }
            if ($helpText -ne '') {
                $fields += @{ Name = $f.Name; Help = $helpText }
            }
        }
    }
    if ($fields.Count -gt 0) {
        Write-Output ""
        Write-Output "CDef: `"$cdefName`" (ContentTableName=`"$tableName`")"
        foreach ($fld in $fields) {
            $h = $fld.Help
            if ($h.Length -gt 250) { $h = $h.Substring(0, 250) + "..." }
            Write-Output "  - FieldName: `"$($fld.Name)`" | HelpDefault: `"$h`""
        }
    }
}

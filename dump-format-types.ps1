# dump-format-types.ps1
# Reflects over all classification DLLs and dumps:
#   1. Any enum types (with integer values for each member)
#   2. Any type/property/field whose name contains "Format" or "Type"

$dlls = @(
    "soa_client_zip_examples\netstandard2.0\Cla0SoaClassificationCommonStrong.dll"
    "soa_client_zip_examples\netstandard2.0\Cls0SoaClassificationCoreStrong.dll"
    "soa_client_zip_examples\netstandard2.0\TcSoaClassificationStrong.dll"
    "soa_client_zip_examples\netstandard2.0\TcSoaStrongModelClassificationCore.dll"
)

foreach ($rel in $dlls) {
    $path = Join-Path $PSScriptRoot $rel
    Write-Host "`n============================================================"
    Write-Host " DLL: $([System.IO.Path]::GetFileName($path))"
    Write-Host "============================================================"

    try {
        $asm = [System.Reflection.Assembly]::LoadFrom($path)
    } catch {
        Write-Host "  LOAD FAILED: $_"
        continue
    }

    $types = $asm.GetTypes()

    # --- 1. All enums ---
    $enums = $types | Where-Object { $_.IsEnum }
    if ($enums) {
        Write-Host "`n  [ENUMS]"
        foreach ($e in $enums) {
            Write-Host "    $($e.FullName)"
            [System.Enum]::GetNames($e) | ForEach-Object {
                $val = [int][System.Enum]::Parse($e, $_)
                Write-Host ("      {0,3} = {1}" -f $val, $_)
            }
        }
    } else {
        Write-Host "`n  [ENUMS] none"
    }

    # --- 2. Types/properties/fields mentioning "format" or "type" ---
    Write-Host "`n  [FORMAT/TYPE members]"
    foreach ($t in $types) {
        $members = $t.GetMembers([System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static') |
            Where-Object { $_.Name -imatch 'format|formattype' }
        foreach ($m in $members) {
            Write-Host "    $($t.FullName).$($m.Name)  [$($m.MemberType)]"
            if ($m.MemberType -eq 'Property') {
                Write-Host "      -> type: $($m.PropertyType.FullName)"
            } elseif ($m.MemberType -eq 'Field') {
                Write-Host "      -> type: $($m.FieldType.FullName)"
            }
        }
    }
}

Write-Host "`nDone."

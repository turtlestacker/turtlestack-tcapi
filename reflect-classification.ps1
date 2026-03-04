# reflect-classification.ps1 — dump all public methods on both Classification services

$dlls = @(
    "soa_client_zip_examples\netstandard2.0\TcSoaClassificationStrong.dll"
    "soa_client_zip_examples\netstandard2.0\Cls0SoaClassificationCoreStrong.dll"
)

foreach ($rel in $dlls) {
    $path = Join-Path $PSScriptRoot $rel
    Write-Host ""
    Write-Host "============================================================"
    Write-Host " DLL: $([System.IO.Path]::GetFileName($path))"
    Write-Host "============================================================"
    $asm = [System.Reflection.Assembly]::LoadFrom($path)
    $svcTypes = $asm.GetTypes() | Where-Object { $_.Name -match 'ClassificationService' }
    foreach ($svc in $svcTypes) {
        Write-Host ""
        Write-Host "  TYPE: $($svc.FullName)"
        $methods = $svc.GetMethods([System.Reflection.BindingFlags]"Public,Instance") |
            Where-Object { $_.DeclaringType -eq $svc } |
            Sort-Object Name
        foreach ($m in $methods) {
            $params = ($m.GetParameters() | ForEach-Object { $_.ParameterType.Name + " " + $_.Name }) -join ", "
            Write-Host "    $($m.Name)($params) -> $($m.ReturnType.Name)"
        }
    }
}
Write-Host ""
Write-Host "Done."

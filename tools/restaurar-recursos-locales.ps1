param([Parameter(Mandatory=$true)][string]$ProyectoAutorizado)
$ErrorActionPreference = 'Stop'
$raizMono = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$origenAutorizado = [IO.Path]::GetFullPath($ProyectoAutorizado)
$inventario = Get-Content -LiteralPath (Join-Path $raizMono 'docs/recursos-locales.json') -Raw | ConvertFrom-Json
foreach ($recurso in $inventario) {
    $fuenteRecurso = [IO.Path]::GetFullPath((Join-Path $origenAutorizado $recurso.ruta))
    $destinoRecurso = [IO.Path]::GetFullPath((Join-Path $raizMono $recurso.ruta))
    if (-not $fuenteRecurso.StartsWith($origenAutorizado + [IO.Path]::DirectorySeparatorChar) -or
        -not $destinoRecurso.StartsWith($raizMono + [IO.Path]::DirectorySeparatorChar)) { throw 'Ruta fuera del proyecto' }
    if (-not (Test-Path -LiteralPath $fuenteRecurso)) { throw ('Falta recurso autorizado: ' + $recurso.ruta) }
    if ((Get-FileHash -LiteralPath $fuenteRecurso -Algorithm SHA256).Hash.ToLowerInvariant() -ne $recurso.sha256) { throw ('El recurso difiere: ' + $recurso.ruta) }
    if ((Test-Path -LiteralPath $destinoRecurso) -and (Get-FileHash -LiteralPath $destinoRecurso -Algorithm SHA256).Hash.ToLowerInvariant() -ne $recurso.sha256) { throw ('El destino difiere; no se sobrescribira: ' + $recurso.ruta) }
}
# Verificar todo antes de copiar. No borrar archivos ni generar GUID nuevos.
foreach ($recurso in $inventario) {
    $fuenteRecurso = Join-Path $origenAutorizado $recurso.ruta
    $destinoRecurso = Join-Path $raizMono $recurso.ruta
    if (Test-Path -LiteralPath $destinoRecurso) { continue }
    New-Item -ItemType Directory -Force -Path (Split-Path $destinoRecurso) | Out-Null
    Copy-Item -LiteralPath $fuenteRecurso -Destination $destinoRecurso
}
Write-Output 'Recursos locales restaurados sin cambiar GUID. Esto NO concede derechos de redistribucion.'

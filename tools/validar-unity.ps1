param([string]$UnityExe = 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe', [switch]$Render)
$ErrorActionPreference = 'Stop'
$raizValidacion = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$proyectoValidacion = $raizValidacion
if (-not (Test-Path -LiteralPath $UnityExe)) { throw 'No se encontro Unity 6000.6.3f1; indicar -UnityExe.' }
# El proyecto no debe estar abierto en otro Editor. No eliminar UnityLockfile.
$argumentosValidacion = @('-batchmode','-nographics','-quit','-buildTarget','Android','-projectPath',('"' + $proyectoValidacion + '"'),
    '-executeMethod','VerificarCopiaPreparacion.Ejecutar','-logFile',('"' + (Join-Path $raizValidacion 'unity-validation.log') + '"'))
if ($Render) {
    # La comprobación de red es asíncrona y cierra Unity al finalizar; no usar -quit.
    $argumentosValidacion = @('-batchmode','-nographics','-buildTarget','Android','-projectPath',('"' + $proyectoValidacion + '"'),
        '-executeMethod','VerificarConexionRender.EjecutarBatch','-logFile',('"' + (Join-Path $raizValidacion 'render-unity-validation.log') + '"'))
}
$assetsProtegidos = @{}
foreach ($relativoProtegido in @('Assets/Settings/UniversalRenderPipelineGlobalSettings.asset','Assets/AulaVirtual/Fonts/MediatecaSDF.asset','Assets/XR/Settings/OpenXR Package Settings.asset','ProjectSettings/BuildProfileUtilityOpenXR.asset')) {
    $rutaProtegida = Join-Path $proyectoValidacion $relativoProtegido
    if (Test-Path -LiteralPath $rutaProtegida) { $assetsProtegidos[$rutaProtegida] = [IO.File]::ReadAllBytes($rutaProtegida) }
}
try {
    $procesoValidacion = Start-Process -FilePath $UnityExe -ArgumentList $argumentosValidacion -WindowStyle Hidden -Wait -PassThru
} finally {
    # Unity puede vaciar atlas dinamicos y runtime settings al cerrar batch. Conservar fuentes.
    foreach ($rutaProtegida in $assetsProtegidos.Keys) {
        $bytesActuales = [IO.File]::ReadAllBytes($rutaProtegida)
        if ([Convert]::ToBase64String($bytesActuales) -ne [Convert]::ToBase64String($assetsProtegidos[$rutaProtegida])) {
            [IO.File]::WriteAllBytes($rutaProtegida, $assetsProtegidos[$rutaProtegida])
        }
    }
}
if ($procesoValidacion.ExitCode -ne 0) { throw 'Unity no completo la validacion. Revisar unity-validation.log; la escena no se guarda.' }
if ($Render) { Get-Content -LiteralPath (Join-Path $raizValidacion 'docs/prueba-render-unity.json') }
else { Get-Content -LiteralPath (Join-Path $raizValidacion 'docs/validacion-escena-unity.json') }

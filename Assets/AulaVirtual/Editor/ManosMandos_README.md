# Manos virtuales con mandos — etapa 1

## Configuración inspeccionada

- Unity 6000.6.3f1, URP 17.6.0, XRI 3.6.1, OpenXR 1.18.0, Input System 1.20.0.
- No están instalados Meta XR Core/Interaction SDK ni XR Hands (tampoco OVRManager/OVRPlugin bajo Assets).
- Meta Quest Support está activado para Android. XRInputModalityManager ya controla Left Controller / Right Controller; los campos Left Hand / Right Hand del prefab están vacíos y no se cambian en esta etapa.
- Capsense completo de Meta requiere componentes de Meta XR Core SDK (documentación: v62+ y condiciones de runtime específicas). No se instala ni activa. La animación actual lee controles expuestos por Input System: trigger, grip y contactos disponibles triggerTouched/primaryTouched/secondaryTouched/thumbstickTouched/thumbrestTouched. No proporciona un esqueleto Capsense de Meta ni seguimiento de dedos reales.

Fuentes oficiales consultadas: [Capsense](https://developers.meta.com/vr/documentation/unity/unity-capsense/), [OpenXR Oculus Touch](https://docs.unity.cn/Packages/com.unity.xr.openxr%401.15/manual/features/oculustouchcontrollerprofile.html) y la documentación instalada XRI 3.6.1 de Hands Interaction Demo. Para etapa 3, esa documentación pide XR Hands >=1.5.1 (recomienda >=1.6.1), Hand Tracking Subsystem, Meta Hand Tracking Aim y la configuración de manos en XRInputModalityManager. Nada de ello se ha instalado/configurado aquí.

## Modelos y aplicación

Se copiaron exclusivamente las mallas rigged oficiales LeftHand/RightHand del sample HandVisualizer de XR Hands 1.6.1, con sus avisos y licencia. El archivo descargado se contrastó con el SHA-1 del registro oficial. Esto NO instala XR Hands: no cambian manifest.json ni packages-lock.json. Ver Models/ControllerHands/PROVENANCE.md.

1. Detén Play y espera a que Unity importe modelos y compile scripts.
2. Abre **Aula Virtual > Manos virtuales con mandos (etapa 1)**.
3. El rig único y Left/Right Controller se resuelven automáticamente. Las raíces visuales esperadas son Left Controller Visual / Right Controller Visual. Si las renombraste, asigna sus raíces de modelo; no asignes el rig completo ni un interactor.
4. Pulsa **Aplicar / actualizar manos**. Primero se guarda una COPIA de la escena abierta en Assets/AulaVirtual/Scenes/BackupsManosMandos. No se guarda la escena principal. Puedes elegir un material URP propio; de lo contrario se crea MAT_ManosMandos (URP/Lit).
5. Usa **Validar manos aplicadas**. Debe haber una raíz ManoVirtual_Mando_v1 en cada controlador, con 15 articulaciones animadas, sin interactores ni TrackedPoseDriver nuevos.

En Editor se conservan los mandos visibles y las manos ocultas. En Play, con tracking válido, se muestran las manos y se fuerza únicamente la invisibilidad de los MeshRenderer/SkinnedMeshRenderer de las raíces visuales originales. No se desactivan sus GameObjects, tracking, haptics, acciones, rayos ni retículas. Deshabilitar/quitar las manos restaura las banderas de render anteriores. Si se pierde el dispositivo, se ocultan las manos y se devuelve el control de visibilidad al modelo original.

La herramienta es idempotente por nodo y admite Undo. **Quitar manos y restaurar mandos** elimina solo sus raíces generadas. No borra FBX ni modelos originales. Los ajustes visuales de muñeca/escala/suavizado se conservan al regenerar.

## Prueba en Editor

Usa **Mando XR (T)** del modo de prueba existente y XR Interaction Simulator en modo Controllers, no Hands. Selecciona el controlador derecho con ] o el izquierdo con [. Apunta fuera de objetos interactivos para probar la animación inicialmente: T mueve el índice; G mueve medio/anular/meñique y, sin sensores capacitivos, parte del pulgar. Las teclas siguen realizando sus acciones originales: G también puede seleccionar/agarrar objetos. El simulador no reproduce necesariamente todos los contactos capacitivos del hardware.

En el componente ManoVirtualConMando puedes activar Probar pose con Inspector durante Play y mover Gatillo/Agarre/Pulgar de prueba; esto solo modifica la apariencia. Desactívalo antes de comprobar mandos reales. Registrar entradas de la mano genera un mensaje puntual con layout, tracking y valores disponibles.

Comprueba ausencia de superposición de mando/mano, seguimiento de ambas muñecas y flexión suave. Prueba selección de videos, Navy Glass, asiento/levantarse, rayos lejanos y teletransporte como antes. No se añade contacto directo con el índice: XR Poke Interactor queda para la etapa 2.

## Calibración y Quest 3S

En cada raíz ManoVirtual_Mando_v1 puedes ajustar desplazamiento, orientación y escala del MODELO, sin tocar Left/Right Controller ni sus orígenes de rayo. La herramienta alinea el Wrist del FBX con ese anclaje, pero el ajuste final depende del agarre físico. Los valores iniciales son aproximados: comprobar tamaño, muñeca, palma y sentido de flexión en Unity/Quest antes de considerarlos finales. Cada articulación permite ajustar eje y amplitud si se requiere una pose diferente.

En Quest verificar gatillos analógicos, agarre y contactos de índice/pulgar disponibles; no asumir que cero significa contacto soportado. Verificar pérdida/recuperación de tracking, cambio de modalidad y que no aparezcan manos flotantes ni duplicadas. Comprobar material sin rosa, piel visible bajo la iluminación del aula, tiempos de frame y todos los flujos existentes. Los dos SkinnedMeshRenderer reutilizan las mallas oficiales y no proyectan sombras; no hay cámaras extra ni blur.

Compilación C# runtime/Editor y estructura/nombres de ambos FBX verificados. La importación nativa, alineación y estética deben comprobarse en Unity y en el dispositivo. No se ejecutó automáticamente la herramienta ni se guardó la escena, ni se generó APK.

Respaldo previo: Backups/ManosMandos_20261031_230545.

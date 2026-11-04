# Manos simuladas y preparación de seguimiento real

Esta etapa añade una herramienta de integración, no ejecuta automáticamente cambios de escena.
El manifest solicita XR Hands 1.8.1. Unity debe resolverlo antes de importar samples.
XRI 3.6.1 necesita XR Hands >=1.8 para reproducir poses en XR Interaction Simulator.
No hace falta Meta XR Interaction SDK ni otro rig/EventSystem.

## Preparación en Unity

1. Detener Play y esperar a Package Manager/compilación. Si XR Hands no aparece, abrir
   Aula Virtual > Manos reales y simuladas y pulsar **1. Resolver XR Hands 1.8.1**.
2. Pulsar **2. Importar samples oficiales**. Se importan HandVisualizer y Hands
   Interaction Demo; no se sobrescriben samples ya importados. Esperar otra compilación.
   Conservar Starter Assets, TMP y Shader Graph existentes. Revisar Project Settings >
   XR Plug-in Management > Project Validation si aparecen requisitos pendientes.
3. Asignar el XR Origin existente. Pulsar **3. Integrar manos en el rig** fuera de Play.
   Primero se guarda una COPIA de la escena actual, incluyendo cambios sin guardar, en
   Assets/AulaVirtual/Scenes/BackupsManosReales. Nunca se guarda la escena principal.
4. Usar **Diagnosticar configuración**: un rig, un EventSystem, un grupo integrado,
   referencias Left/Right Hand, Poke y NearFar. Repetir integración actualiza solo el grupo
   identificado ManosReales_Aula_v1. Undo restaura cambios de jerarquía/referencias.

El prefab de ejemplo se abre en una escena temporal aislada. Solo se trasladan las
ramas Left Hand, Right Hand y Hand Visualizer bajo Camera Offset. Las referencias
externas se remapean al rig existente; una referencia no resoluble cancela la operación.
No se importan a la escena la cámara, locomoción, gravedad ni rig del demo.
XRInputModalityManager alterna las ramas originales de mandos y las nuevas de manos.
Los grupos oficiales coordinan Poke y NearFar; no se conectan listeners nuevos a botones.

## Prueba sin Quest ni cámara de seguimiento

- Activar el modo Mando XR existente para habilitar simulador/entrada XR. Entrar en Play
  y enfocar Game. La webcam del PC no se utiliza: son poses simuladas.
- En la interfaz de XR Interaction Simulator cambiar el modo de interacción a Hands
  (no confundir con Controller/FPS), seleccionar mano izquierda o derecha y moverla
  usando los controles que muestra el propio simulador. F/Y seleccionan manipulación
  izquierda/derecha; Tab cambia el dispositivo seleccionado.
- F8 activa la pose Poke, M Pinch y F9 Open. Se usa una copia local de las acciones
  de manos: ManosSimuladasAula.inputactions. No se editan los samples originales.
  También se actualiza la referencia del InputActionManager del simulador para habilitar
  esta copia. N/O se mantienen para levantarse/regresar al aula.
- Con manos apuntar al pizarrón y hacer Pinch sobre Actualizar; verificar respuesta
  visual y una sola consulta, sin entrar al cine.
- Sentarse mediante la interacción del asiento existente (o B como apoyo de prueba).
  Acercar el índice al botón de Biblioteca del Navy Glass con Poke, atravesar ligeramente
  la superficie y retirarlo. Verificar un único clic, abrir/minimizar controles y reproducir.
  El Poke solo llega a UI físicamente cercana; para el pizarrón se usa NearFar/pinza.
- Verificar Levantarse desde barra, controles y biblioteca. Comprobar también el cine
  por su botón propio y las funciones de descarga/caché habituales.
- Alternar Hands/Controllers: deben permanecer visibles únicamente las manos de la
  modalidad activa. Los mandos conservan sus manos virtuales, tracking, acciones y rayos.
  Probar ambas manos y pérdida/recuperación simulada de tracking.

No se cambian atajos, asientos, teletransporte ni bibliotecas para realizar estas pruebas.
Si una acción de asiento dependía exclusivamente de un botón de mando, se debe registrar
ese caso para una adaptación explícita de accesibilidad; no se reasigna silenciosamente.

## Preparación opcional de OpenXR y comprobación posterior

El botón **Preparar OpenXR para Quest / Link** se ejecuta por separado, fuera de Play.
Respalda la escena y los assets OpenXR antes de habilitar Hand Tracking Subsystem y
Meta Hand Tracking Aim en configuraciones existentes Android/Standalone. Guarda solo
esos assets de configuración, sin cambiar loaders ni perfiles de mandos. Estos features
no son necesarios para simular manos en Editor. Comprobar Meta Quest Support en Android
y conservar los perfiles actuales de mandos antes de compilar para el visor.

Con Quest 3S habrá que validar permisos/configuración de seguimiento, precisión física
de índice/pinza, oclusión, iluminación, manos fuera de campo, cambio real mandos/manos,
compatibilidad del runtime y rendimiento. La simulación no certifica estas condiciones.
No se genera APK en esta etapa ni se promete reconocer manos reales en el PC sin visor.

## Retirada y estado de validación

Fuera de Play, **Retirar únicamente las manos integradas** crea copia de escena,
restaura referencias previas del gestor de modalidad y acciones originales del simulador,
y borra solo las raíces identificadas. Es reversible con Undo. No borra samples/paquetes
ni modifica features compartidos de OpenXR.

Se verificó compilación C# de los archivos propios runtime/Editor con referencias de
Unity 6000.6.3f1. La ejecución batch de Unity terminó con código 1 al abrir el proyecto;
no permitió validar resolución/importación ni Play Mode en esta sesión. Esos pasos
deben comprobarse en el Editor con la secuencia anterior antes de guardar la escena.
No se ejecutó el integrador automáticamente. El respaldo inicial está en
Backups/SeguimientoManos_20261031_232854.

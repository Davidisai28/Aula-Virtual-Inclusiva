# Panel personal Navy Glass

## Barra compacta, controles y biblioteca personal — versión actual

Fuera de Play abre **Aula Virtual > Panel personal Navy Glass > Aplicar / actualizar (con respaldo)**. No regeneres la biblioteca del pizarrón ni repitas Preparar XR. El generador exige servicios de catálogo/caché existentes enlazados al mismo VideoPlayer del controlador. No crea servicios, reproductores, EventSystem ni módulos. Conserva distancia, altura, desplazamiento lateral, ancho y ajuste de orientación del panel anterior, además del ancho propio de biblioteca. La escena principal no se guarda.

Tres vistas excluyentes: BarraCompacta, VistaControles y BibliotecaCercana. Sentarse abre solo la barra, colocada en la zona inferior del espacio personal previamente ajustado. Controles conserva su diseño multimedia y añade Minimizar. Biblioteca es una segunda vista de BibliotecaVisualAula, con el mismo CatalogoVideosDrive y GestorCacheVideos que usa el pizarrón; comparte consulta, disco, operación activa y reproductor. No hay segundo catálogo de servicio ni lógica HTTP duplicada.

La biblioteca personal tiene dos filas por página, nombres sin extensión, caché verificada, Descargar/Reproducir, progreso, errores y Actualizar. Al abrirla se solicita actualizar mediante el servicio compartido, que impide consultas simultáneas. Si no hay servidor, utiliza el respaldo de catálogo y archivos locales válidos. Volver y Levantarse están fuera de los controles que se bloquean durante consultas o descargas.

La selección personal notifica a PanelPersonalNavyGlass qué video espera. Cuando ese video empieza a reproducirse, vuelve a la barra si la biblioteca seguía abierta. Cambiar de vista no pausa ni detiene el video y no cancela descargas. Al levantarse se oculta toda la raíz y se desconectan sus listeners; los servicios compartidos pueden continuar una operación iniciada. La vista del pizarrón conserva su comportamiento previo, incluida su barra y el cine.

El cálculo de pose del panel no cambia. No se recoloca al abrir/cerrar vistas ni sigue la cabeza. La biblioteca se centra sobre esa pose con `anchoBibliotecaMetros` configurable (1 m inicialmente); su escala no altera `anchoMetros` de controles/barra. Los fondos pasan a opacidad 99–99,5 %, sin blur ni cámaras auxiliares. La raíz de biblioteca puede extenderse fuera del rectángulo compacto: por diseño, su validador usa sus propios límites de 1600×900.

Prueba en Editor con el backend local:

1. Sentarse: solo Biblioteca / Controles / Levantarse. Girar la cabeza no debe arrastrar el panel.
2. Abrir Controles y pulsar Minimizar durante reproducción: el video debe continuar. Verificar Reproducir, Pausar, Reiniciar y Detener con sus métodos originales.
3. Abrir Biblioteca: debe consultar los videos reales. Probar paginación con al menos tres entradas reales, sin crear ejemplos.
4. Descargar desde una fila: debe aparecer porcentaje, bloquear nuevas selecciones y, al empezar el video en el pizarrón, regresar a la barra. Repetir con el segundo video. Volver durante descarga debe seguir disponible; se puede reabrir para ver el progreso.
5. Después de una consulta y descarga válidas, detener Node.js y reabrir biblioteca: debe mostrar videos locales completos y reproducirlos sin descargar.
6. Levantarse desde barra, controles y biblioteca, también durante consulta/descarga: debe recuperar el comportamiento de cámara anterior y ocultar todas las vistas personales. Repetir sentado varias veces para comprobar ausencia de listeners duplicados.
7. Probar biblioteca del pizarrón y Entrar al cine desde sus controles existentes. Deben conservar video y RenderTexture. Repetir interacciones en los modos Mouse y Mando XR del Editor.

Compilación de runtime/Editor verificada y regiones principales de controles/biblioteca comprobadas sin solapamientos calculados. Falta la prueba integral de interacción y reproducción en Play. No se aplicó automáticamente el generador, no se guardó la escena ni se generó APK.

Respaldo de esta etapa: `Backups/PanelCompactoBiblioteca_20261031_224438`. Las instrucciones históricas que siguen sobre Biblioteca deshabilitada quedan sustituidas por esta sección.

## Aplicación

1. Fuera de Play abre **Aula Virtual > Panel personal Navy Glass**.
2. Asigna el gestor `PruebaAsientoXR` del asiento. Si hay uno solo, se encuentra automáticamente. El controlador se deduce de los targets persistentes de los botones originales del panel; también puedes asignar el `ControladorVideoXR` existente explícitamente.
3. Pulsa **Aplicar / actualizar (con respaldo)**. Se respalda la escena abierta, incluidos cambios sin guardar, en `Assets/AulaVirtual/Scenes/BackupsPanelPersonal`. La escena principal no se guarda.
4. Usa **Previsualizar desde el asiento** para verlo en Scene y **Validar panel y conexiones** para comprobar renderers, límites y targets. La previsualización activa el objeto solo para inspección en Editor; al entrar en Play se oculta hasta sentarse.
5. Guarda manualmente la escena únicamente cuando aceptes los cambios.

La herramienta solo reemplaza raíces `PanelPersonal_NavyGlass_v1` propias, hijas del gestor de asiento. Conserva ajustes de posición del panel anterior. No borra los botones 3D originales ni sus eventos. **Restaurar presentación anterior** quita el enlace opcional y únicamente el panel nuevo, con respaldo y Undo; la interfaz original vuelve en el próximo Play.

## Funcionamiento

`PruebaAsientoXR` conserva exactamente las operaciones de orientación/traslado al sentarse y levantarse. Su actualización de interfaz tiene un enlace opcional al nuevo panel: cuando existe, oculta Menú, Levantarse y panel 3D antiguos, y muestra el panel personal completo. Levantarse queda siempre accesible en el panel nuevo, independiente de descargas o estado multimedia. Sin ese enlace se ejecuta la presentación original.

Los botones nuevos invocan persistentemente `ControladorVideoXR.Reproducir`, `Pausar`, `Reiniciar`, `Detener` y `PruebaAsientoXR.LevantarseDesdeBoton`. No se añade un reproductor ni se cambian sus parámetros, RenderTexture, EventSystem, XR Origin, cine, catálogo ni caché. El panel solo lee nombre y estado de reproducción cada 0,25 s.

Biblioteca permanece deshabilitada y sin listeners. `BibliotecaCercana_Reservada` es un RectTransform vacío e inactivo para la etapa posterior. No se consulta Drive desde el panel ni se instancia otro catálogo.

## Posición

Selecciona la raíz nueva y ajusta `PanelPersonalNavyGlass`: distancia horizontal delante de la mirada (0,85 m), altura respecto a los ojos (-0,28 m), desplazamiento lateral, ancho (0,78 m) y ajuste de orientación Euler. La altura resultante es aproximadamente 0,40 m.

Se coloca una vez al sentarse, con orientación hacia los ojos. Es hijo del gestor del asiento, no de la cabeza ni del XR Origin; Update solo actualiza texto y no mueve el panel. Para aplicar ajustes durante Play usa el menú contextual **Recolocar panel durante Play**. En Editor usa Previsualizar nuevamente. Evita colocarlo detrás de la mesa o paredes: el raycaster respeta oclusión 3D.

## Diseño

Canvas World Space 1200×620 con escala mundial compensada; CanvasScaler local y TrackedDeviceGraphicRaycaster, sin otro EventSystem. Paleta Navy #0B1F3A/#102B4E/#19395F, blancos suaves y foco azul hielo; uGUI y TextMeshPro, fuente SDF propia de la mediateca con acentos/elipsis. Cuatro controles principales con iconos vectoriales y dos acciones secundarias. Vidrio simulado mediante el shader existente, material propio `MAT_UI_NavyGlass.mat`, transparencias moderadas y sin blur, cámara extra o cambios globales de URP.

## Comprobación en Play

- Antes de sentarse: panel oculto. Usa el selector de silla existente o B para sentarte; debe aparecer debajo de la mirada, completo y con Levantarse disponible.
- Mueve y gira la cabeza: el panel debe quedarse fijo en el mundo. Revisa que no tape el pizarrón y que sea accesible desde una postura cómoda.
- Con un video seleccionado en la biblioteca principal, comprueba título/estado y Reproducir, Pausar, Reiniciar y Detener. Prueba en los modos Mouse y Mando XR existentes.
- Comprueba que Biblioteca no abre nada todavía. Levantarse debe funcionar incluso durante una descarga; debe ocultar el panel y recuperar la posición original del estudiante.
- Repite varias veces el ciclo de sentarse/levantarse: no debe duplicar paneles ni listeners. Comprueba también la biblioteca principal y el cine como antes.

Compilación runtime/Editor comprobada; diez regiones principales sin superposición calculada y algoritmos originales de traslado conservados. La comodidad, oclusión y resultado visual se validan en Unity y después en Quest. No se ejecutó automáticamente la herramienta ni se guardó la escena.

Respaldo previo de archivos y escena: `Backups/PanelPersonalNavy_20261031_213338`.

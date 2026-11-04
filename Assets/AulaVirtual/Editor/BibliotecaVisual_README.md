# Biblioteca visual del aula — etapa 1

## Vidrio ahumado visionOS y correcciones de disposición — versión actual

Fuera de Play (Stop, no Pause), abre **Aula Virtual > Biblioteca visual > Generar o actualizar**. Se crea respaldo y se mantienen referencias a catálogo, caché, cine y cámara del componente anterior. No repitas Preparar XR si ya funciona. El generador no guarda la escena principal.

Se corrige una regresión: MostrarEstado movía ListaVideos a anchoredPosition cero, tapando la cabecera. Ahora solo se restablece el scroll si hay un ScrollRect antiguo real; el contenedor paginado permanece en su posición. Validar interfaz ahora comprueba también superposición entre las regiones reales de cabecera, lista y pie, incluso durante Play.

El diseño añade etiqueta Aula Virtual / Mediateca, título de 68 unidades, subtítulo de 40 y Actualizar arriba a la derecha. La lista comienza en y=304 y contiene dos tarjetas de 216 unidades, con 24 de separación. Los nombres conservan 48 unidades, hasta dos líneas; metadatos y acciones usan 40. El pie conserva conteo, paginación y cine. No usa máscara ni ScrollRect nuevos.

Paleta: fondo #161A20, panel #1F232A, tarjetas #242A33 con elevaciones #303640. Texto #F5F7FA/#D7DEE7 e información #AEBBC9. Progreso turquesa; caché menta identificada además por texto; foco XR azul hielo. Los botones son cápsulas con transición breve de luminosidad y borde; no modifican eventos ni material compartido al apuntar.

El shader convierte a lineal las paletas enviadas por UV, que antes no recibían la conversión de COLOR y producían un degradado gris lavado. El fondo incluye una base opaca que cubre las esquinas, evitando ver el video por detrás; las tarjetas tienen opacidad 96–98 %. No hay captura de pantalla, blur, segunda cámara ni refracción real del aula. El Canvas cubre los bounds del pizarrón con escala mundial uniforme y 8 mm de separación; no se cambian URP ni RenderTexture.

Al regenerar se crea/reutiliza **Assets/AulaVirtual/Fonts/MediatecaSDF.asset**, fuente SDF local a la biblioteca con atlas inicial de 1024, muestreo de 90, caracteres españoles y U+2026 (elipsis). No se modifica LiberationSans SDF global ni paquetes. La fuente, atlas y material se guardan como assets propios; la escena principal no se guarda. El generador verifica que exista el glifo antes de sustituir la UI. Esto resuelve el aviso “character used for Ellipsis is not available”. El asset es dinámico para otros caracteres compatibles con la fuente origen.

Comprobación:

1. Regenerar tras compilar e importar shader; limpiar Console y entrar en Play. Deben verse cabecera completa y ambas tarjetas debajo, sin avisos de elipsis ni MissingComponentException.
2. Actualizar varias veces y usar Anterior/Siguiente si hay más de dos videos: la lista no debe desplazarse sobre el título. Pulsar Validar interfaz en Play para medir los RectTransform reales, no solo coordenadas previstas.
3. En Scene, seleccionar la raíz y encuadrar con F, inspeccionar la colocación sobre la malla. En Game, maximizar y usar resolución nativa/1x; probar desde varios asientos y ángulos mediante el sistema existente, sin cambiar su configuración.
4. Comprobar foco, pulsación y estado deshabilitado en Mouse y Mando XR; verificar descarga/progreso, video desde caché, Volver a biblioteca y entrada al cine sin reinicio. Guardar manualmente solo cuando se acepte.

Respaldo previo: `Backups/VisionOSBiblioteca_20261031_211457`. Se compilaron los scripts; la importación nativa de fuente/shader y la validación visual final se realizan en Unity al regenerar.

## Diseño comercial minimalista — versión actual

Regenera fuera de Play con **Aula Virtual > Biblioteca visual > Generar o actualizar**. La herramienta crea primero una copia de la escena abierta y conserva Undo; no guarda la escena principal. Conserva las referencias del componente anterior a catálogo, caché, cine y cámara. No repitas Preparar XR cuando la interacción ya funciona. No se modifica el backend, asientos, teletransporte, reproductor ni RenderTexture.

El Canvas se ajusta a los bounds de la malla de PantallaVideo_Aula y su escala mundial. Mantiene proporción 16:9 y un margen del 2 %, compensando escala heredada. Diseño lógico 1600×900: fondo carbón opaco; título de 68 unidades; nombres de 48; información y acciones de 40. En un pizarrón de 1,91×1,07 m, un nombre tiene aproximadamente 5,6 cm de cuerpo tipográfico. Esto no garantiza legibilidad a cualquier distancia o resolución: comprobar desde el asiento en Game y posteriormente en Quest.

Se eliminan ondas, miniaturas decorativas, cabecera cápsula, iconos y textos repetidos del pie. Las superficies ahumadas y botones tienen bordes de bajo contraste y reflejos mínimos; el verde salvia es el único acento. No hay blur, cámara adicional ni cambios globales de calidad. Las tarjetas incluyen una sombra discreta sin filtro de desenfoque.

Distribución: cabecera y acción Actualizar; una línea de estado separada; dos tarjetas de 236 unidades con 24 de separación dentro de un área de 496; pie con total de videos, Anterior / indicador / Siguiente cuando hay más de dos y Entrar al cine. No se usa ScrollRect ni RectMask2D en el nuevo listado.

El filtro offline se aplica antes de paginar. Solo se instancian dos filas, se limita el índice si cambia el catálogo y se bloquea navegación durante consulta/descarga/preparación. Actualizar vuelve a la primera página; Volver a biblioteca conserva la página mientras siga disponible. El nombre visible oculta exclusivamente la extensión final .mp4 (sin importar mayúsculas), sin modificar el nombre ni metadatos originales. Los títulos usan hasta dos líneas, tamaño fijo y elipsis para nombres excepcionalmente largos.

Comprobación en Unity:

1. Regenerar y ejecutar Validar interfaz: comprueba renderers y límites del Canvas, también en Play. Revisar que no haya Console errors ni paneles rosas al importar el shader.
2. Actualizar y probar los dos videos reales. Los títulos deben ocupar hasta dos líneas; tamaño, disponibilidad y acción tienen zonas separadas.
3. Con al menos tres videos reales, comprobar Anterior/Siguiente y total general (no solo filas de página); no se añaden videos ficticios para esta prueba.
4. Comprobar descarga/progreso, caché offline, reproducción, regreso a biblioteca y cine en ambos modos de entrada. La paginación debe bloquearse mientras el gestor está ocupado.
5. Revisar a resolución Game nativa/1x desde la posición real del estudiante. Guardar manualmente solo al aceptar el diseño.

Verificación realizada en código: compilación runtime y Editor, 146 comprobaciones de paginación/nombres, nueve regiones principales dentro del Canvas sin superposición, y altura de dos líneas calculada con las métricas del font SDF actual. Falta la validación visual del resultado y del shader en Unity; no se ejecutó automáticamente el generador.

Respaldo previo: `Backups/BibliotecaMinimalista_20261031_204717`. Las instrucciones históricas siguientes describen versiones anteriores.

## Nitidez y shader de vidrio

La captura con todo el pizarrón ocupando aproximadamente 570 píxeles de ancho reduce un cuerpo de 32 unidades a unos 11 píxeles (y el glifo visible a menos). Aumentar Dynamic Pixels Per Unit no aumenta la resolución del panel Game ni del atlas SDF. Para evaluar: maximiza Game, selecciona 1920×1080 o superior y escala 1x; compara desde el asiento, sin confundir zoom de Scene con resolución de render.

La ventana del generador dispone de **Diagnosticar nitidez (sin cambios)**, también en Play: informa resolución de cámara/render, ancho proyectado del pizarrón y cuerpo de texto aproximado en píxeles. La medida no incluye el escalado posterior de la ventana Game ni el de una captura adjunta.

Regenera fuera de Play después de importar `Shaders/UI_LiquidGlassAula.shader`. Se crea/reutiliza `Materials/MAT_UI_LiquidGlassAula.mat`, aplicado exclusivamente a los paneles de la biblioteca. El Canvas añade canales UV1/UV2/UV3 para sus parámetros. El shader calcula silueta redondeada, borde suavizado con derivadas, gradiente y reflejos; soporta stencil, RectMask2D y macros estéreo. No es refracción real ni blur del aula, y no captura otra cámara. No se activa Opaque Texture ni se cambia URP, MSAA, Render Scale, calidad global o RT_VideoAula.

El texto mínimo pasa a 36 unidades y los nombres a 42 con dos líneas; las ondas pierden el relleno plano intenso y se desvanecen hacia el fondo. Verifica en Unity que el shader importe sin errores, sin paneles rosas, que los bordes y textos se aprecien y que funcione el recorte al desplazar la lista. La compilación C# no verifica un shader ni garantiza el resultado visual en Quest.

Respaldo previo: `Backups/NitidezVidrio_20261031_203529`. No se ha guardado ni regenerado automáticamente la escena principal.

## Diseño Mediateca basado en la referencia visual

Para aplicar el nuevo aspecto, fuera de Play usa **Aula Virtual > Biblioteca visual > Generar o actualizar**. La herramienta respalda la escena abierta, sustituye únicamente su raíz identificada y conserva Undo. No guarda la escena principal. No repitas Preparar XR si ya funciona.

El nuevo diseño conserva la superficie 1600×900: cabecera cápsula, título grande, ondas turquesa vectoriales, tarjetas de 198 unidades (dos visibles, con scroll para las demás), miniaturas decorativas, insignias Disponible/En caché y botón de cine destacado. Los nombres admiten dos líneas. Los estados y botones se siguen alimentando con el catálogo y la comprobación real de caché.

Las miniaturas son ilustraciones de ondas diferenciadas por ID, no fotogramas reales. No se modifica el aula para reproducir el entorno, luces ni mandos de la imagen de referencia. El efecto de vidrio usa degradados y bordes geométricos sin blur, texturas generadas, cámara extra ni cambios de URP. Los elementos decorativos no reciben raycasts.

En Play actualiza el catálogo y revisa ambos videos, los estados, progreso, reproducción y regreso a la biblioteca. La barra de reproducción conserva sus acciones. Valida legibilidad desde el asiento y el diseño en Unity: la compilación no sustituye esa revisión visual.

Respaldo anterior al rediseño: `Backups/RedisenoMediateca_20261031_202843`.

## Biblioteca funcional: descarga, reproducción y entradas de prueba

Esta sección sustituye las instrucciones históricas siguientes que describían filas sin reproducción.

No es necesario regenerar ni repetir Preparar XR si la biblioteca actual ya recibe clics. Al entrar en Play se configuran las filas con Descargar o Reproducir tras verificar caché. El gestor se toma de la referencia existente CatalogoVideosDrive.gestorCache; conserva ReproductorVideo_Aula y RT_VideoAula. No se guarda automáticamente la escena.

### Mouse y XR en Editor

Menú **Aula Virtual > Biblioteca visual > Prueba Editor > Mouse** o **Mando XR (T)**. Se puede elegir antes o durante Play. Fuera de Play solo se guarda una preferencia del Editor, sin cambiar la escena. En Play se alternan fuentes dentro del único XRUIInputModule: Mouse activa ratón directo y deshabilita entrada XR y el componente del simulador; Mando XR hace lo contrario. No se añaden módulos ni se cambian Input Actions. Estos cambios no forman parte del build Quest. Su finalidad es impedir que el clic se interprete a la vez como ratón y gatillo simulado.

En Mouse, enfoca Game y pulsa con el botón izquierdo. En Mando XR, selecciona Controllers, apunta con el mando y pulsa/suelta T. Los gatillos físicos de Quest siguen usando las acciones UI Press ya probadas. Evita cambiar de modo mientras mantienes un botón presionado.

### Descarga y reproducción

Arranca Node.js y actualiza el catálogo. Pulsa Descargar en el primer video: se bloquean otras selecciones y Actualizar mientras se muestra el porcentaje (actualizado como máximo cada 0,1 s). Al finalizar se verifica el tamaño, guarda una revisión del archivo y prepara el VideoPlayer existente. El menú se oculta cuando el reproductor está listo; permanece una barra compacta con Volver a biblioteca y Entrar al cine sobre el pizarrón.

Volver a biblioteca pausa el video y muestra nuevamente el catálogo. Selecciona el segundo video y verifica que cambien contenido y título de estado. Los controles existentes de pausa, reinicio, detener y volumen no se modifican. Seleccionar una fila no llama al gestor de cine. En error, se muestra el detalle en el encabezado/Console y la fila ofrece Reintentar. Si expiró la preparación (45 s), el gestor desbloquea la selección.

Solo hay una descarga/preparación activa: pulsaciones rápidas sobre otros videos se ignoran mientras dura la operación. Cada operación usa una copia de los metadatos y una identidad propia, para que una coroutine anterior no interrumpa una preparación posterior. Los listeners se retiran al desactivar/destruir sus componentes.

### Caché y prueba offline

Console distingue `[CACHE-VIDEO] Descarga guardada:` de `[CACHE-VIDEO] Usando caché, sin descargar:`. Descarga ambos videos, vuelve a la biblioteca y detén Node.js. Actualizar debe mostrar SIN CONEXIÓN con los videos locales. Selecciona cada uno: debe reproducirse sin descargar. Repite después de salir y entrar en Play para comprobar persistencia.

Las descargas nuevas se guardan en BibliotecaVideos como `id_hash(size|modifiedTime).mp4`. Un cambio de metadatos genera otra revisión; no se borran videos anteriores. Los temporales de la operación se limpian en error/cancelación. Si se encuentra un archivo final incompleto en la ruta de destino, se preserva con sufijo antes de guardar la descarga correcta. Los MP4 legacy `id.mp4` siguen siendo compatibles por tamaño hasta que se guarda una revisión nueva; su fecha original es desconocida y no permite detectar con certeza una actualización del mismo tamaño.

### Cine y comprobación de aceptación

Reproduce el primer video y usa Entrar al cine de la barra: la pantalla curva debe mostrar el mismo contenido y tiempo, sin nueva descarga ni reinicio. Regresa con el control existente del cine y repite con el segundo. Volver a biblioteca pausa la reproducción; Entrar al cine no la reinicia ni crea otro reproductor.

Comprueba ambos modos de entrada, clics rápidos, fallo de red durante descarga, reintento y reproducción offline, sin teletransporte accidental ni MissingComponentException. La compilación y las pruebas de política de archivos no sustituyen esta prueba integral en Play con el servidor y los videos reales.

Respaldo previo a esta etapa: `Backups/BibliotecaFuncional_20261031_200230`. La interfaz y la barra nuevas se crean en memoria durante Play. Guarda manualmente únicamente los cambios que decidas conservar fuera de Play.

## Catálogo real de Drive (etapa actual)

No es necesario regenerar ni repetir Preparar XR si la interfaz del pizarrón ya funciona. Tras compilar, BibliotecaVisualAula localiza el único CatalogoVideosDrive de su escena en Play (también puedes asignarlo explícitamente en Inspector). Usa exclusivamente su consulta HTTP existente; no descarga videos ni llama al reproductor. Se conservan los controles XR. Las secciones históricas de prueba local que siguen ya no describen la acción Actualizar actual.

Al iniciar Play se retiran los ejemplos. Actualizar llama a ConsultarBiblioteca, se deshabilita mientras carga y crea una fila por ID válido, con nombre literal, tamaño en B/KiB/MiB/GiB y disponibilidad sin comprobar. Las acciones de fila permanecen deshabilitadas. Los listeners se conectan y desconectan con la UI. Las respuestas con IDs duplicados o entradas inválidas se rechazan sin reemplazar el respaldo.

Cada respuesta válida (incluido catálogo vacío) se guarda como `catalogo-videos-drive.json` en Application.persistentDataPath. Se escribe primero un temporal y se reemplaza el archivo anterior; una respuesta inválida o un fallo HTTP no sobrescribe el último válido. Se guarda la dirección del servidor para evitar cargar un respaldo de otro origen.

Ante fallo de conexión se muestra SIN CONEXIÓN. HTTP de error o JSON inválido se muestran como ERROR. En ambos casos solo se listan entradas del último catálogo cuyo `.mp4` exista en BibliotecaVideos y coincida con el tamaño esperado mediante GestorCacheVideos.EstaEnCache. Archivos `.part`, incompletos o de tamaño desconocido no se consideran caché. La comprobación es por tamaño, igual que el gestor actual, no por hash. Sin respaldo y sin caché verificada, la lista queda vacía con aviso. Un archivo en caché que no figure en el último catálogo no se puede identificar para esta lista.

Prueba en Editor:

1. Arranca tu servidor Node.js en http://127.0.0.1:3000. Abre /api/videos para confirmar que devuelve un objeto con `videos`.
2. Entra en Play: debe aparecer el estado neutral, sin ejemplos ni afirmaciones de descarga sin comprobar.
3. Pulsa Actualizar con el mando simulado: CARGANDO, luego CONECTADO con el número de videos. Console: `[CATALOGO-DRIVE] Catálogo recibido: N videos.` Comprueba nombres y tamaños en el pizarrón.
4. Repite varias veces, incluso con pulsaciones rápidas: una sola consulta activa y sin acumulación de filas. Las filas no deben reproducir ni descargar nada.
5. Tras obtener un catálogo válido, detén el servidor y vuelve a actualizar: SIN CONEXIÓN y solo videos completos presentes en caché. Repite después de salir y volver a Play para verificar persistencia. No borres el respaldo ni los videos para esta prueba.
6. Si el servidor devuelve un error HTTP o JSON inválido, debe aparecer ERROR y conservarse el último catálogo válido. Reinicia el servidor y actualiza: debe recuperarse.

Si prefieres regenerar, el generador actual solo crea la plantilla inactiva y el estado neutral, sin ejemplos. No pulses Preparar XR: no es necesario modificar EventSystem ni teletransporte para esta etapa. El generador conserva su respaldo previo y no guarda la escena principal.

## Prueba de nitidez e interacción XR (actualización)

Fuera de Play: generar de nuevo y después pulsar **Preparar interacción XR (con respaldo)**. Cada operación crea una copia previa. No se guarda la escena principal. Si la UI ya existe, regenerar es necesario para aplicar la nueva tipografía y distribución.

El Canvas utiliza escala mundial uniforme compensando la escala no uniforme del Quad, margen del 2 %, CanvasScaler World Space (Dynamic Pixels Per Unit = 2), tipografía SDF de al menos 32 unidades y padding adicional. La lista tiene 456 unidades de alto, con filas de 144 y dos separaciones de 12: las tres filas de ejemplo caben completas. Los avisos inferiores usan textos abreviados y cajas más altas. Dynamic Pixels Per Unit no aumenta la resolución del atlas SDF ni la del visor; la mejora principal es el tamaño angular del texto. Se conserva RT_VideoAula y no se cambia calidad global.

Preparar XR requiere un EventSystem existente y una biblioteca; no crea otro. Retira módulos competidores con Undo y deja XRUIInputModule. Desactiva la entrada directa de ratón de ese módulo para aislar el clic del mando simulado. Añade/configura TrackedDeviceGraphicRaycaster, asigna la cámara del XROrigin y conserva oclusión 3D. Deshabilita el XRSimpleInteractable SOLO del pizarrón y retira exclusivamente su listener persistente GestorCineXR.EntrarAlCine. Conserva el collider, controles de asientos, rayos de teletransporte y reproductor. Conecta el cine mediante BibliotecaVisualAula; sus listeners runtime se retiran en OnDisable para no duplicarlos.

Se habilita UI Interaction en NearFarInteractor; los XRRayInteractor que ya tenían UI habilitada conservan esa función. Los rayos de teletransporte con UI deshabilitada no se cambian. Se respetan fuentes de entrada personalizadas y referencias existentes; solo se completan referencias faltantes de UI Press/UI Press Value con las de Starter Assets para la mano correspondiente.

### Verificación antes de APK

1. En Inspector del EventSystem debe quedar solo XRUIInputModule, enabled y Enable XR Input activo. No debe repetirse el aviso de dos módulos.
2. En cada Near Far de la mano izquierda/derecha: UI Interaction activo, UI Press y UI Press Value asignadas a XRI Left/Right Interaction. El Input Action Manager del rig debe incluir XRI Default Input Actions. En Play usa **Diagnosticar XR**: UI Press debe informar acción enabled=True; si no, revisa dicho manager sin modificar los assets de acciones.
3. El prefab XR Interaction Simulator ya está referenciado en la escena guardada. Asegúrate de que haya una sola instancia activa. Entra en Play y enfoca Game. Usa su modo Controllers, no Hands. Tab alterna FPS/Controller; `]` selecciona el dispositivo derecho. Mantén botón derecho del ratón y mueve para orientar el dispositivo. Apunta el rayo a Actualizar y presiona/suelta **T** (Trigger). También puedes usar clic izquierdo en el modo Point And Click del simulador; el módulo UI no recibe el ratón directamente. Estos controles provienen del sample 3.6.1 instalado; el panel del simulador muestra el dispositivo/modo seleccionado.
4. Al apuntar, el botón debe tomar acento turquesa; al mantener el gatillo, oscurecerse; al soltar sobre él, debe aparecer exactamente `[BIBLIOTECA-XR] CLICK ACTUALIZAR recibido. Prueba local; sin petición a Drive.` y cambiar el estado a CLIC RECIBIDO. Repetir 3 veces debe producir tres mensajes, no listeners acumulados. Puedes desactivar Collapse en Console.
5. Pulsar Actualizar, botones de fila o fondo no debe mover el rig. Solo el botón Entrar al cine llama al gestor desde esta UI; usa el control existente Volver al aula para regresar. Los atajos U/O del GestorCineXR original permanecen: evita usarlos durante la prueba; O también puede coincidir con el gesto Open si cambias a manos en el simulador.
6. Comprueba nitidez en Game a resolución nativa del panel (escala 1x), desde la posición real de ojos del asiento, sin acercar artificialmente la cámara. Comprueba descripciones, letras con descendentes y el pie en las tres vistas. El diagnóstico avisa de textos desbordados. El zoom de Scene no demuestra legibilidad desde el asiento ni la nitidez final del visor.
7. Regenera dos veces: una sola raíz BibliotecaVisual_Aula_v1 y un solo raycaster en ella. Vuelve a preparar XR (es idempotente), prueba clic, Undo y Redo fuera de Play. Revisa Console sin MissingComponentException. Guarda manualmente solo cuando aceptes el resultado. La comprobación final en Quest sigue siendo necesaria para confirmar nitidez y ergonomía reales.

La generación/eliminación visual no revierte las interacciones preparadas: usa Undo del paso Preparar XR o la copia previa si deseas recuperar la selección original del pizarrón y el módulo anterior.

Las secciones históricas siguientes describen la etapa visual inicial; la prueba local y Preparar XR anteriores sustituyen sus indicaciones de botones sin acciones y ausencia de raycaster.

1. Abre `Assets/AulaVirtual/Scenes/AulaPrincipal_GLB.unity` y espera a que compile.
2. Abre **Aula Virtual > Biblioteca visual > Generar o actualizar**.
3. Asigna `PantallaVideo_Aula` o deja que se encuentre por nombre.
4. Elige Biblioteca, SinConexion o Descargados y pulsa **Generar / actualizar interfaz**.
5. En Scene, inspecciona el objeto seleccionado desde el frente del pizarrón. Desactiva Gizmos para juzgar el diseño. El Canvas ocupa el Quad y se separa 8 mm en su eje local negativo Z.
6. Repite con las tres vistas. Solo debe existir una raíz `BibliotecaVisual_Aula_v1` bajo la pantalla. Actualizar reconstruye esa raíz y descarta ajustes manuales realizados dentro de ella.
7. **Eliminar únicamente interfaz** borra exclusivamente esa raíz identificada mediante su componente marcador. Ambas operaciones admiten Undo. Guarda manualmente la escena cuando aceptes el resultado.

Los títulos, tamaños y porcentajes son ejemplos. `BibliotecaVisualAula` expone referencias a Content, plantilla de fila inactiva, botones y estados para la integración posterior. El Content usa VerticalLayoutGroup y ContentSizeFitter, dentro de ScrollRect y RectMask2D. No hay consultas de red ni listeners funcionales. La interfaz no instala raycasters ni modifica EventSystem.

El vidrio es una aproximación visual ligera mediante esquinas redondeadas, degradados y reflejo superior, sin refracción ni desenfoque del aula. No requiere shaders, texturas ni cámaras nuevas.

La primera validación debe hacerse en Scene, fuera de Play: se conservan intencionalmente el teletransporte al seleccionar la pantalla y la reproducción automática anteriores. La interacción XR se configurará en otra etapa.

La copia previa de la escena y su meta está fuera de Assets, en `Backups/AulaPrincipal_GLB_BibliotecaVisual_20261031`, para evitar importar un GUID duplicado. Para restaurar, cierra la escena y copia ambos archivos a su ubicación original.

## Corrección de CanvasRenderer y comprobación manual

Las operaciones actuales de generar y eliminar crean además una copia de la escena abierta en `Assets/AulaVirtual/Scenes/BackupsBibliotecaVisual` mediante SaveScene con saveAsCopy. Esto incluye cambios sin guardar y no guarda la escena principal.

La ventana dispone de **Validar interfaz (sin cambios)**: examina todos los Graphic, incluidos inactivos, sin invocar el getter que puede crear automáticamente CanvasRenderer. Informa rutas de jerarquía completas y detecta también el renderer destruido retenido en el campo no serializado de Graphic.

Para retirar una versión anterior, usa **Eliminar únicamente interfaz** fuera de Play. La herramienta completa los renderers y resincroniza las referencias antes de desactivar y destruir la raíz propia. No elimines CanvasRenderer por separado. Después limpia Console, genera y valida.

Comprobación de aceptación en Unity 6.6:

1. Generar Biblioteca y pulsar Validar; inspeccionar esquinas, textos y barras.
2. Regenerar SinConexion, Descargados y Biblioteca; verificar una única raíz identificada.
3. Desactivar y activar la raíz, y activar/desactivar PlantillaFilaVideo y ProgresoDescarga para comprobar callbacks de máscara.
4. Eliminar desde la herramienta, deshacer y rehacer. Generar, deshacer y rehacer. Tras cada restauración, pulsar Validar y comprobar Console con Collapse desactivado.
5. Confirmar ausencia de MissingComponentException durante repintados de Scene/Game y que eliminar no deja raíces propias.

El ajuste del campo privado `Graphic.m_CanvasRenderer` está limitado al Editor y se basa en el código uGUI instalado. No modifica el paquete. Si una actualización cambia ese campo, la herramienta cancela la preparación con un error explícito. El callback de Undo solo resincroniza raíces identificadas de la biblioteca; no genera interfaces.

No se ha ejecutado automáticamente este ciclo de aceptación; debe verificarse explícitamente desde el Editor.

# Miniaturas de la biblioteca

Se conserva la paginación de dos videos por página, el tamaño del Canvas y los botones.
La fila sigue midiendo 1472 × 216 unidades: miniatura 256 × 144 a la izquierda, título
de 48 en dos líneas, metadatos debajo y botón 300 × 100 a la derecha. Se aplica también
a las filas de la biblioteca personal Navy Glass. Los nombres originales no cambian.

## Backend

El catálogo incorpora el campo opcional `thumbnailPath`, relativo al mismo servidor.
El endpoint `GET /api/videos/:id/thumbnail` comprueba catálogo, carpeta y permisos actuales
antes de devolver la imagen. No expone `thumbnailLink`, claves ni tokens a Unity.

Drive proporciona enlaces temporales que pueden necesitar autenticación:
https://developers.google.com/workspace/drive/api/reference/rest/v3/files
El proxy permite únicamente HTTPS en subdominios de googleusercontent.com y rechaza
redirecciones. Acepta PNG/JPEG de hasta 256 KiB, dos solicitudes simultáneas, caché en
memoria de hasta 32 imágenes por 60 segundos y utiliza los límites existentes de
peticiones y transferencia. No usa disco persistente de Render ni servicios adicionales.

No se descargan MP4 para crear portadas. Si Drive no tiene miniatura, aparece VIDEO.
El catálogo y la reproducción siguen funcionando si las imágenes fallan.

## Unity

`MiniaturaVideoAula` crea objetos uGUI reales: fondo redondeado, Mask, RawImage y marcador
neutral. CanvasRenderer existe antes de activar los gráficos. La imagen no recibe raycasts.
Los generadores reutilizan esa misma construcción; no se agregan objetos repetidos.

Las filas ya guardadas se adaptan al crearse en Play. **No hace falta regenerar las interfaces**
ni cambiar conexiones del Inspector. Fuera de Play, la escena conserva su presentación guardada.
Los generadores incorporan el nuevo diseño si se usan después; no se ejecutan automáticamente.

Solo se cargan las imágenes de las filas visibles, con máximo dos solicitudes activas y
sin peticiones simultáneas para la misma clave. Las texturas se liberan al cerrar/cambiar
de página. El guardado comprimido está en `Application.persistentDataPath/MiniaturasAula`,
con hasta 128 imágenes, identificadas por servidor, ID y modificación del video.
`BibliotecaVideos` y el catálogo existente no se borran ni se migran.

## Probar primero en Editor

1. Detener Play y esperar la compilación. Si Unity no detecta los cambios, usar Assets >
   Refresh (Ctrl+R). Conservar `AulaPrincipal_GLB` abierta.
2. Opcional: ejecutar Aula Virtual > Biblioteca visual > Validar miniaturas sin cambiar escena.
   Crea una fila temporal separada y comprueba componentes, regiones, botón y paginación;
   no guarda escenas ni es una certificación visual de XR.
3. Para probar el backend nuevo antes de actualizar Render, iniciar `npm start` en backend
   usando las credenciales externas existentes y seleccionar `entornoEditor: Local` en
   `Assets/AulaVirtual/Resources/BibliotecaBackend.json` antes de entrar en Play.
4. Pulsar Actualizar. Deben aparecer las portadas reales. Abrir la biblioteca personal y
   comprobar títulos, botones, volver, levantarse y reproducción como antes.
5. Con más de dos videos, pasar de página y volver: las imágenes deben corresponder a cada fila.
6. Después de una consulta y carga de imágenes, desconectar la red y reiniciar Play:
   los videos descargados y sus miniaturas guardadas deben seguir disponibles.
7. Volver a `entornoEditor: Remoto` al terminar. Android sigue usando HTTPS remoto siempre.

El Render anterior es compatible: sin `thumbnailPath` muestra el marcador neutral.
Para ver imágenes desde Render debe estar desplegada la nueva versión del backend.
No cambiar secretos, plan, Root Directory ni comandos de inicio por esta funcionalidad.

## Pruebas realizadas

- Compilación C# de runtime y Editor con las referencias de Unity 6.6.
- 16 pruebas Node satisfactorias, incluidas las anteriores de catálogo/descargas y las nuevas
  de miniaturas, permisos, caché del proxy, tamaño, formato y destinos permitidos.
- Prueba real local con la cuenta de servicio externa autorizada: catálogo HTTP 200,
  dos portadas JPEG HTTP 200 de 10.867 y 16.657 bytes. No se registraron enlaces privados.
- La escena principal conserva su SHA-256 respecto del respaldo.
- Validación ejecutada en el Editor abierto: `[MINIATURAS-TEST] OK`, sin errores ni avisos;
  fila real del generador, sin duplicados ni superposición de regiones, componentes completos.
- Render actualizó automáticamente el backend tras publicar el commit en main. Se comprobó
  su catálogo HTTP 200 y ambas imágenes JPEG HTTP 200, con los mismos tamaños de la prueba local.
- Prueba en Play en el Editor: `[MINIATURA-AULA] RED` confirmó las dos cargas desde
  https://aula-virtual-inclusiva.onrender.com y se observaron ambas portadas en el pizarrón.
  Persisten avisos previos de MediatecaSDF por U+2026 (puntos suspensivos); TMP recurre a
  Truncate. Las imágenes sí cargaron, sin errores. La corrección del atlas queda separada
  de esta etapa; no se modificó la fuente compartida.

Pruebas reproducibles del backend:

```powershell
cd backend
npm test
# Con GOOGLE_APPLICATION_CREDENTIALS configurada hacia el JSON externo existente:
node tools/smoke-thumbnails.js
```

La validación visual en el visor físico Quest 3S sigue pendiente. No se implementaron
360°, YouTube, cambios de XR ni un nuevo reproductor en esta etapa.

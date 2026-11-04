# Procedencia, licencias y restauración

Revisión: 1 de noviembre de 2026. No se concede una licencia nueva al material de terceros.

## Aula Dutch Classroom

Origen confirmado por el propietario y ficha del proveedor:
[Dutch Classroom, Lars-Nieuweboer](https://www.cgtrader.com/free-3d-models/interior/other/dutch-classroom).
La ficha indica Royalty Free License (No AI). Classroom.glb y classroom.fbx proceden de ese
recurso según el propietario; no se descargó de nuevo ni se usó para entrenar modelos.
Los [términos de CGTrader](https://www.cgtrader.com/pages/terms-and-conditions), secciones
21 y 21B, limitan redistribución de fuentes y permiten usos incorporados bajo condiciones;
No AI añade restricciones sobre entrenamiento. El [centro de ayuda](https://help.cgtrader.com/hc/en-us/articles/360015124437-Royalty-Free-License)
distingue producto incorporado de entrega del archivo fuente.

La sección 20.5 contempla compartir dentro de una misma entidad jurídica si esa entidad es
la adquirente de la licencia. No se verificó que ese sea nuestro caso ni quiénes serían los
colaboradores. Por prudencia no se versionan las fuentes. Cada colaborador debe disponer de
acceso legítimo; si no está cubierto por la misma entidad/licencia, obtener su propia licencia
o permiso escrito del autor/CGTrader. No se contactó a nadie. Uso educativo no elimina restricciones.

## Floor.png y Ceiling.png

El GLB contiene imágenes llamadas Floor y roof1. La comparación SHA-256 encontró que Floor.png
es EXACTAMENTE igual a la imagen Floor embebida en classroom.glb. Eso demuestra correspondencia
con ese GLB, no por sí solo autoría original ni derechos adicionales.
Ceiling.png NO coincidió byte a byte con ninguna imagen embebida; su procedencia exacta no está
verificada. Tampoco hubo coincidencia exacta al comparar píxeles decodificados RGBA, no solo
el contenedor PNG. No afirmar que sea roof1 ni que sea un recurso propio. También quedan pendientes
EmergencyExit, keyboard y wood; se excluyen preventivamente junto con los modelos.
Ver resultados reproducibles en historico/preparacion_20261101/procedencia-texturas.json e inventario recursos-locales.json.
La comparación de píxeles está en historico/preparacion_20261101/comparacion-pixeles-texturas.json; no se editaron imágenes.

## Restauración sin perder GUID

El proyecto original conserva los recursos restringidos; Git los ignora y los originales no se borran.
Antes de abrir un clon nuevo, el usuario autorizado debe obtener el modelo desde la ficha
original, guardar justificante/licencia y disponer de las conversiones/texturas autorizadas.
No renombrar recursos ni generar metadatos nuevos sobre las rutas existentes.

Si ya tiene una copia local autorizada con los mismos archivos y .meta, cerrar Unity y ejecutar:

```powershell
./tools/restaurar-recursos-locales.ps1 -ProyectoAutorizado 'C:/ruta/al/proyecto-autorizado'
```

El script comprueba hashes de todos los recursos antes de copiar, conserva .meta, no sobrescribe
archivos existentes ni publica nada. No sustituye la licencia. El inventario contiene hashes y
rutas, no el contenido. Si se descarga una versión distinta, importar en un proyecto temporal
y revisar conversión/rutas/GUID antes de reemplazar; no prometer una reconstrucción automática.
Sin restauración legal, un clon no es una copia visual completa del aula.

## Otros recursos

Las manos de mandos incluyen LICENSE, avisos y PROVENANCE en Models/ControllerHands.
Los samples XRI/XR Hands mantienen sus avisos; se añadieron las licencias de los paquetes
correspondientes en docs/licencias-unity. No se reasignan como código propio.
LiberationSans conserva OFL en Assets/TextMesh Pro/Fonts. Las dependencias Node se restauran
desde npm con sus avisos; no se distribuye node_modules. Los materiales y scripts nuevos no
amplían derechos de texturas ajenas. No se verificó documentalmente la licencia de los videos
de Drive: el profesor debe confirmar permisos antes de compartirlos. No se versiona MP4.

## Archivos grandes / LFS

Classroom.glb ≈95.8 MiB, Floor.png ≈50.3 MiB, Ceiling.png ≈32.3 MiB y MP4 legacy ≈46.6 MiB.
Se excluyeron por derechos/duplicación de videos, no se comprimieron ni eliminaron. No se
activó LFS. Si se autoriza versionar recursos grandes, revisar cuota Free y configurar LFS
antes del primer commit; no esperar al rechazo de un push ni contratar almacenamiento.

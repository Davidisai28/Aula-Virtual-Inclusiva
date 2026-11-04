# Validación de la raíz original

Etapa: 4 de noviembre de 2026. Los resultados actuales se guardan en resultados-pruebas.json,
validacion-escena-unity.json y auditoria-git.json. Los informes del 1 de noviembre están en
historico/preparacion_20261101 y no se presentan como pruebas nuevas.

## Comprobaciones de esta etapa

- Respaldo externo verificado por hash antes de integrar.
- Comparación de los tres proyectos y preservación de todos los fuentes Unity originales.
- Un único Git activo, main, sin remoto, commits ni repositorio activo dentro de PreparacionGitHub.
- Instalación reproducible en backend/ y pruebas HTTP con Node 24.
- Comprobación real de health, catálogo y descarga usando la cuenta de servicio autorizada,
  sin copiar el JSON y descartando el MP4 después de comprobar longitud/cabecera.
- Compilación y apertura estructural de AulaPrincipal_GLB desde la ruta original con el
  comprobador batch, sin guardar la escena. Los resultados definitivos se adjuntan al terminar.
- Auditoría de candidatos Git, exclusiones de licencias, secretos y tamaño de archivos.

## Alcance de Unity

Resultados finales: apertura/compilación batch desde la ruta original con salida 0;
sin scripts faltantes y sin gráficos uGUI sin CanvasRenderer. Un rig, un EventSystem y
un VideoPlayer con RT_VideoAula; catálogo, caché, cine y asientos presentes. Los 1444
archivos Unity previos conservaron sus hashes. No se guardó la escena.
Backend: npm ci restauró 89 paquetes; las 10 pruebas pasaron con Node 24.22.1.
Prueba real: health/catalog/download HTTP 200, 2 videos, descarga completa de 11295626
bytes y cabecera MP4 válida; sin guardar el video ni copiar credenciales.

El informe estructural comprueba scripts faltantes, uGUI sin CanvasRenderer, rig, EventSystem,
VideoPlayer, RT, catálogo, caché, cine y asientos. Distingue TextMeshPro 3D, que usa MeshRenderer.
No equivale a una prueba visual de Play Mode. No se ejecutan generadores ni se cambia de rig.
La fuente SDF, URP y ajustes XR se protegen frente a normalizaciones de cierre batch.
Al finalizar se comprueban los hashes del baseline; cualquier diferencia ajena se investiga.

## Secuencia manual posterior

Abrir el mismo proyecto original y probar Actualizar, descarga, imagen/audio en el pizarrón,
cache sin backend, panel personal, sentarse/levantarse y cine independiente. Revisar mouse,
mandos y manos por separado. No declarar comprobado Quest por una simulación de Editor.

## Pendientes

Aprobar repositorio privado; decidir autenticación o política de videos públicos; verificar
cuentas y cuotas sin facturación; autorizar Render y obtener URL HTTPS real. Probar arranque
en frío y conexión remota desde Editor antes de autorizar APK. Quest 3S físico, precisión de
Poke/pinza y funcionamiento con la computadora apagada requieren pruebas posteriores.
Ceiling y permisos de colaboradores para recursos CGTrader conservan sus pendientes de licencia.

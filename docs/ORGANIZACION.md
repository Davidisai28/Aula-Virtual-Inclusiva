# Organización definitiva: 4 de noviembre de 2026

La fuente de trabajo es C:/U/Aula_Virtual_Inclusiva, con Unity directamente en la raíz y backend/
al lado de Assets, Packages y ProjectSettings. Este es el único Git activo del proyecto, en main.
No se añadió remoto ni se hizo commit/push. No se desplegó Render ni se generó APK.

## Respaldo previo externo

C:/U/Respaldos_AulaVirtual/OrganizacionRaiz_20261104_165837 conserva los fuentes Unity originales,
el backend y la preparación administrativa. Se verificaron 1444 archivos Unity con hash, sin
diferencias, antes de integrar. No se copió el JSON privado de la cuenta de servicio.
El Git anterior, sin commits ni remoto, se conserva como GitPreparacionArchivado en ese respaldo.
No se borraron PreparacionGitHub ni C:/U/AulaVirtualBackend. Son referencias históricas;
no continuar desarrollando allí ni sincronizarlas con el salón.

## Comparación y decisión

Los scripts, escena y recursos Unity comunes no requerían reemplazo. La fuente SDF del original
es más reciente. La preparación tenía desactivados Meta Quest Support y perfiles Touch que
estaban activos en el original, además de una diferencia en BuildProfileUtilityOpenXR.
Se conservaron TODOS los archivos originales de Assets, Packages y ProjectSettings.
El único archivo Editor añadido es un verificador explícito de solo lectura, con su .meta.
No se copiaron escenas, interfaces, modelos, materiales o ajustes XR desde la preparación.

El backend original y el preparado tenían idéntico código/dependencias; solo la preparación
incluía .node-version. Se integraron los fuentes del backend y esa selección Node 24, sin
node_modules, .env privados ni credenciales. Las dependencias se restauran con npm ci.

## Archivos incorporados/adaptados

README, docs/, tools/, backend/, .gitignore, .gitattributes y render.yaml están en la raíz.
Los patrones de exclusión y las herramientas ya no esperan una carpeta unity/.
PreparacionGitHub, Backups y carpetas generadas se ignoran. Se mantuvieron las exclusiones
de modelos CGTrader, texturas pendientes, videos, claves, APK y cachés de ejecución.
La restauración legal de recursos conserva rutas Assets/ y GUID originales.

La documentación de Unity está en docs/UNITY.md. Los informes de noviembre 1 se conservaron
en docs/historico/preparacion_20261101; no son resultados de esta reorganización.
Los resultados actuales se registran en docs/resultados-pruebas.json y VALIDACION.md.
El inventario baseline-unity-organizacion.json permite verificar que los 1444 archivos
existentes antes del cambio permanecen iguales al finalizar las comprobaciones.

## Trabajo futuro

Abrir siempre esta ruta original en Unity Hub y usar backend/ para Node. Guardar normalmente
los cambios que se quieran registrar y revisar git status desde la raíz. No hay pasos de copia
ni sincronización manual. Publicación privada y despliegue requieren autorización posterior.

# Unity en la raíz original

Abrir `C:/U/Aula_Virtual_Inclusiva` en Unity Hub, con 6000.6.3f1. No abrir una subcarpeta
unity ni seguir editando PreparacionGitHub. Assets, Packages y ProjectSettings no se movieron.
La escena es Assets/AulaVirtual/Scenes/AulaPrincipal_GLB.unity. Las versiones de paquetes,
XR, interfaces, modelos y materiales originales se conservaron. No reimportar samples sobre
las modificaciones existentes ni reemplazar el XR Origin/EventSystem.

## Prueba en Editor

1. Iniciar el backend desde la subcarpeta backend; comprobar configuración local y credenciales.
2. Esperar importación/compilación y revisar Console.
3. Abrir la escena principal y entrar en Play. Probar Actualizar, descarga y reproducción.
4. Probar sentarse, Navy Glass, biblioteca personal, controles y levantarse.
5. Entrar/salir del cine sin perder el video; probar caché con el backend detenido.
6. Probar mouse, simulador XR y manos por separado. Quest físico y precisión real siguen pendientes.

La configuración está centralizada en Assets/AulaVirtual/Resources/BibliotecaBackend.json:
Local utiliza http://127.0.0.1:3000; Remoto requiere URL HTTPS real. Android siempre usa Remoto.
No inventar URL, no poner claves en este archivo. Detener Play antes de cambiar entorno.
El cambio de URL necesita una primera consulta válida; los videos descargados no se borran.

## Compilación estructural sin guardar escena

Con el proyecto cerrado en otros Editores, desde la raíz:

```powershell
./tools/validar-unity.ps1
```

No elimina archivos de bloqueo ni ejecuta generadores. Abre la escena con API de Editor,
comprueba scripts/CanvasRenderer/rig/EventSystem/reproductor/RT y escribe un informe en docs.
Restaura los bytes de fuente TMP, URP y ajustes OpenXR que Unity pueda normalizar al cerrar.
No sustituye comprobar imagen, audio, rayos e interacciones en Play Mode.

## APK, pendiente de autorización

Antes del build: derechos de recursos, backend HTTPS publicado con acceso aprobado, URL real
configurada y pruebas de Editor remoto. Revisar Android Build Support, ARM64/IL2CPP, firma
local, permisos de internet y Project Validation de OpenXR/Meta Quest, conservando lo existente.
El validador Android bloquea un build sin URL HTTPS. No se genera APK automáticamente.
No versionar keystore/contraseñas. Después, probar en Quest con la computadora apagada,
catálogo, descarga, imagen/audio, caché sin internet y alternancia de mandos/manos.

Los recursos CGTrader no se publican: un clon requiere obtención/restauración autorizada
con sus .meta antes de abrirlo. El salón local completo sigue intacto.

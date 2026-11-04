# Seguridad y acceso

Las credenciales permanecen fuera de los proyectos y del monorepositorio. La ruta privada
autorizada por el propietario se utiliza únicamente como GOOGLE_APPLICATION_CREDENTIALS
en pruebas; no se copia aquí ni se documenta su nombre. El JSON original no se modifica.
No introducir claves, correo privado de cuenta de servicio, tokens o contraseñas en APK,
Resources, escenas, capturas de Console, README, render.yaml o historial Git.

El backend acepta ADC/GOOGLE_APPLICATION_CREDENTIALS o JSON mediante variable privada del
proveedor. Preferir Secret File de Render y apuntar a /etc/secrets/google-service-account.json.
No imprimir errores crudos de Google: pueden contener detalles de petición/autorización.

## API pública y autenticación pendiente

El ID de Drive y la URL no son secretos de acceso. CORS no autentica una APK. HTTPS protege
transporte, no autoriza usuarios. El rate limit reduce abuso pero no controla estudiantes.
La versión preparada NO implementa identidad de estudiantes. Para evitar publicar una API
accidentalmente, producción arranca solo si se autoriza explícitamente material público con
ALLOW_PUBLIC_CATALOG=true. La plantilla deja false. No habilitar para videos privados.

Propuesta para aprobación: login Google del estudiante con flujo nativo/OIDC, token corto
verificado por el backend (emisor, firma, audiencia y expiración), lista de usuarios o dominio
permitidos en configuración privada, y autorización en ambos endpoints. El client ID de
OAuth es público; no usar un secreto compartido incrustado. No requiere diseñar una segunda
caché ni una base de datos si la lista es pequeña. Comprobar requisitos y condiciones vigentes
del proveedor de identidad antes de implementarlo. No se añadieron autenticación, Firebase
ni otro servicio sin autorización.

## Medidas implementadas

Lectura de carpeta directa autorizada, verificación actualizada de permisos antes de media,
ID con longitud/formato acotados, catálogo paginado/caché de metadatos, límites por IP y
concurrencia, cancelación upstream, longitud de archivo y ausencia de logs de secretos.
Los límites se mantienen en memoria y no son protección distribuida. En Render se confía
en un hop de proxy; confirmar el formato real X-Forwarded-For antes de abrir a usuarios.
No se promete defensa completa contra DDoS ni acceso autenticado.

## Auditoría local

Ejecutar desde la raíz `python tools/auditar.py`; revisar docs/auditoria-git.json. Solo muestra
tipo de hallazgo, ruta y línea, nunca el secreto. Es una búsqueda heurística de claves privadas,
credenciales JSON, tokens GitHub y claves Google en candidatos Git. Los binarios y archivos
mayores de 5 MiB se inventarían, no se interpretan como texto. Complementar con revisión
manual y protección de secretos de GitHub antes de cualquier push.
No había carpetas .git en los proyectos originales ni en C:/U; no hay historial anterior
detectado que escanear o reescribir. La raíz original tiene un único Git activo, sin commits ni remoto. El Git previo de la preparación, sin historial, se archivó fuera del proyecto.
Autenticarse con GitHub mediante su mecanismo seguro del sistema; no pegar tokens PAT en
la URL del remoto ni incluirlos en comandos/documentación versionada.

No se considera expuesta una clave solo por existir en un archivo privado fuera de Git.
Si alguna se compartió previamente en otros repositorios/chats públicos, revocarla y crear
otra antes del despliegue; borrar el archivo de un commit no revoca la clave.

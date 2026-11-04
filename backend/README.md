# Backend de la biblioteca

Node.js **24 LTS** y npm. Express y googleapis se fijan mediante package-lock.json.
El código original y este backend conservan `/api/videos` y `/api/videos/:id/download`.

```powershell
npm ci
Copy-Item .env.example .env
# Editar .env local: ID real de carpeta y ruta privada del JSON autorizado.
npm start
```

El servidor escucha `process.env.PORT` o 3000 en `0.0.0.0`. No abrir puertos del router
para las pruebas locales. La API no exige autenticación en desarrollo; usar red de confianza.
Si ya existe otro servidor en 3000, detenerlo de forma controlada o usar otro PORT.
El ID anterior de carpeta queda como compatibilidad solo en desarrollo; producción exige
`GOOGLE_DRIVE_FOLDER_ID`. `.env` nunca se versiona y `npm start` lo carga si existe.

## Google Drive

Compartir SOLO la carpeta autorizada con el correo de la cuenta de servicio como lector.
El backend usa el scope `drive.readonly`; no concede acceso de editor ni necesita facturación.
`GOOGLE_APPLICATION_CREDENTIALS` apunta al JSON fuera del repositorio. No pegarlo en Unity.
La alternativa `GOOGLE_SERVICE_ACCOUNT_JSON` solo se introduce en variables privadas del
proveedor. No activar delegación de dominio. El scope limita operaciones a lectura; el ACL
de Drive determina a qué archivos puede acceder esa cuenta.

La consulta pagina la carpeta directa (no subcarpetas), almacena metadatos en memoria por
60 segundos y comparte consultas simultáneas. Antes de descargar vuelve a verificar carpeta,
tipo MP4, archivo no eliminado y permiso de descarga. Una URL arbitraria no puede descargar
otros archivos de Drive. Los errores no incluyen claves ni mensajes crudos del proveedor.

Streaming mediante pipeline, sin MP4 persistentes ni buffers de archivo completo. Envía tipo,
longitud y nombre; una interrupción cancela la conexión a Drive y un stream incompleto falla.
No se implementa reanudación Range; Unity reintenta la descarga completa si falta caché válida.
Límites actuales: 30 solicitudes/minuto/IP, dos descargas globales, una por IP y presupuesto
de 256 MiB/hora por proceso, incluyendo reservas fallidas. Los contadores se reinician al
reiniciar Render y NO sustituyen sus cuotas mensuales ni son garantía de cero cargos.
No hay base de datos, disco persistente, cron ni keep-alive artificial.

## Pruebas

```powershell
npm test
# Prueba real optativa; usa la ruta privada sin imprimir el JSON:
$env:GOOGLE_APPLICATION_CREDENTIALS = 'C:/ruta/privada/cuenta-servicio.json'
node --env-file-if-exists=.env tools/smoke-real.js
```

`smoke-real.js` inicia un servidor solo en loopback, consulta health y catálogo, descarga
el video más pequeño por streaming, comprueba bytes y cabecera MP4 y lo descarta.
No guarda videos. Esto consume una descarga real; no ejecutarlo repetidamente sin necesidad.

## Render Free, aún NO desplegado

Mismo repositorio privado, rama `main`, Root Directory `backend`, Build `npm ci`, Start
`npm start`, health `/health`, Node 24 LTS, instancia **Free**. La raíz backend limita los
cambios que desencadenan builds; el filtro adicional incluye `backend/**`. El template
render.yaml deja autodeploy desactivado para revisión manual de cada publicación.

Variables: NODE_ENV=production, GOOGLE_DRIVE_FOLDER_ID real,
GOOGLE_APPLICATION_CREDENTIALS=/etc/secrets/google-service-account.json. Crear ese Secret
File en el panel, NO copiarlo al repositorio. Las variables/secret files son capacidades
documentadas de Render, sin requisito de instancia de pago indicado.

El inicio de producción está bloqueado por defecto: `ALLOW_PUBLIC_CATALOG=false`.
Antes de autorizar un endpoint público, decidir autenticación o confirmar que TODOS los
videos pueden exponerse públicamente y establecer explícitamente `true`. Un repositorio
privado y una URL difícil de adivinar NO protegen la API. No incrustar una clave en la APK.
Consultar docs/SEGURIDAD.md y docs/GRATUIDAD.md en la raíz antes de cualquier despliegue.
No introducir tarjeta, no habilitar facturación y no subir de plan para resolver cuotas.

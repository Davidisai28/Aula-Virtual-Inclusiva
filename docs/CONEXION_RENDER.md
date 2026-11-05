# Biblioteca conectada a Render

Servidor real: https://aula-virtual-inclusiva.onrender.com

La configuración pública es `Assets/AulaVirtual/Resources/BibliotecaBackend.json`:

```json
{
  "entornoEditor": "Remoto",
  "urlLocal": "http://127.0.0.1:3000",
  "urlRemota": "https://aula-virtual-inclusiva.onrender.com",
  "compartirCatalogoGuardado": true
}
```

Editor está preparado para consultar Render directamente, sin iniciar Node local.
Para desarrollar contra localhost: detener Play, poner entornoEditor en Local e iniciar
el backend local. Android siempre utiliza urlRemota HTTPS, aunque Editor esté en Local.
Los campos servidor antiguos del Inspector no sustituyen esta configuración central.
No se introdujeron credenciales en Unity; solo se guardan URLs públicas.

## Catálogo guardado y caché

Local y Remoto sirven la misma biblioteca autorizada. compartirCatalogoGuardado permite
leer el último catálogo de cualquiera de los dos orígenes configurados, incluso si todavía
indica localhost. Solo se muestran offline los videos cuya caché real esté verificada.
No se hace fallback de red a localhost. Si se usan carpetas Drive diferentes en desarrollo,
desactivar esa opción para no mezclar metadatos de bibliotecas distintas.

Se conserva el directorio BibliotecaVideos y el cálculo de nombres/versiones existentes.
GestorCacheVideos no se modifica: la selección sigue comprobando caché antes de descargar.
Una consulta fallida conserva el último catálogo válido; no borra videos.

## Espera inicial y reintento

La consulta HTTPS admite 120 segundos. Utiliza UnityWebRequest asíncrono: la interfaz
permanece disponible y el botón Actualizar evita consultas simultáneas. Después de 10 segundos,
el mensaje cambia a Esperando al servidor. La interfaz usa ese mensaje real del catálogo.
Ante fallo, se conserva disponibilidad offline y Actualizar permite reintentar.
No se añadieron pings de mantenimiento ni solicitudes periódicas para despertar Render.
Un HTTP 200 con contenido inválido no se presenta como catálogo conectado.

## Distinguir red de catálogo guardado en Unity

1. Mantener Node local apagado, seleccionar Remoto y entrar en Play en la escena habitual.
2. Pulsar Actualizar y esperar si Render estaba inactivo.
3. Console debe mostrar CONSULTA RED con la URL https://aula-virtual-inclusiva.onrender.com/api/videos.
4. Solo una respuesta válida genera HTTP 200 · RED con esa URL y el número de videos.
   CATÁLOGO GUARDADO es un mensaje diferente y no acredita conexión a Render.
5. Desconectar internet después de una consulta válida y volver a probar los videos guardados.
   Comprobar el log Usando caché, sin descargar. No borrar datos de la aplicación.

## Comprobación automatizada explícita

Con Unity cerrado, desde la raíz:

```powershell
./tools/validar-unity.ps1 -Render
```

También hay un menú Aula Virtual > Biblioteca visual > Verificar Render sin reproducir,
fuera de Play. Estas acciones realizan una consulta y una descarga real; no ejecutarlas
repetidamente sin necesidad porque consumen tráfico del plan gratuito.

El verificador crea objetos temporales en una escena de previsualización separada. Ejecuta
la misma coroutine del catálogo y las mismas lecturas de caché, sin abrir ni guardar el aula.
Comprueba que el puerto local 3000 rechace conexiones. Usa UnityWebRequest/DownloadHandlerFile
para descargar el video más pequeño a un temporal del sistema, fuera de Assets y de la caché.
Comprueba HTTP, tamaño, Content-Length cuando existe, cajas MP4 ftyp/moov/mdat y SHA-256
contra una copia completa previa de caché. Sin esa referencia no declara integridad confirmada.

El temporal propio se elimina al terminar. Los archivos de caché se comparan por hash antes
y después, y se restaura el catálogo persistido previo. No se crean reproductores ni se cambia
la escena. El informe es docs/prueba-render-unity.json y el log local render-unity-validation.log.
La prueba inspecciona transporte y lógica en Editor; no certifica reproducción audiovisual,
legibilidad ni interacción XR. Si la consulta fue rápida, no prueba un arranque en frío real:
la espera y el timeout quedan preparados, pero el ensayo de inactividad se comprueba aparte.

## APK posterior

La URL ya queda preparada para Android y el validador exige HTTPS. No se generó APK en esta
etapa. Faltan las pruebas de imagen/audio, caché sin internet, controles y manos en Quest 3S,
con computadora apagada. El backend y su configuración de acceso en Render no se modificaron.

## Resultado medido: 4 de noviembre de 2026

La ejecución nativa de Unity terminó correctamente y compiló los scripts. El puerto 3000
rechazó la conexión después de detener el backend local. Render respondió HTTP 200 con
dos videos en 33,78 segundos, dentro del timeout de 120 segundos. La duración no identifica
por sí sola la causa de la espera ni certifica una prueba visual del mensaje de carga.

La descarga respondió HTTP 200: 11.295.626 bytes esperados y recibidos, estructura MP4 válida
y SHA-256 coincidente con la copia previa. Los dos videos quedaron reconocidos mediante
lectura offline; todos los archivos de caché conservaron sus hashes y se restauró el catálogo
persistido anterior. Detalle reproducible en [prueba-render-unity.json](prueba-render-unity.json).
La escena AulaPrincipal_GLB conservó su SHA-256 respecto del respaldo previo.

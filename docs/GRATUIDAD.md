# Condiciones de coste y límites

Revisión documental: **1 de noviembre de 2026**. No se accedió a paneles de facturación,
no se creó servicio y no se introdujo tarjeta. Las condiciones deben revisarse otra vez
el día del despliegue; esta preparación NO garantiza disponibilidad permanente gratuita.

## Render

Elegir workspace Hobby e instancia Free. La documentación indica suspensión por inactividad
tras 15 minutos, arranque aproximado de un minuto y 750 horas gratuitas/mes por workspace.
Sin método de pago, agotar el tráfico incluido suspende servicios; con método de pago
puede producir cargos. No asociar tarjeta ni activar una instancia/disco de pago.
Si el alta o una verificación exige tarjeta/autorización bancaria, detenerse: no se ha
verificado el alta de esta cuenta.
El plan Hobby vigente incluye 5 GB de tráfico: contrastar el valor efectivo del panel.
Fuentes: [Free](https://render.com/docs/free),
[cambios de planes](https://render.com/docs/new-workspace-plans),
[FAQ](https://render.com/docs/faq).

Render también puede suspender un Free por tráfico elevado iniciado hacia APIs/almacenamiento
externos. El proxy de videos de Drive está expuesto a esa limitación, cuyo umbral no publica.
La recuperación documentada exige un plan de pago: NO hacerlo bajo la restricción actual.
Si sucede, detener el uso remoto y mantener reproducción de caché; revisar otra arquitectura
gratuita antes de continuar. No evadir límites con pings, cuentas nuevas o reinicios.
Por ello Render Free es una preparación para piloto, no garantía de servicio a una clase.

Con 5 GB, un video de unos 56 MB consume del orden de 90 descargas, antes de otros usos.
El presupuesto de la aplicación no es un contador mensual de Render y se pierde al reiniciar.
Las cuotas del proveedor y la ausencia de método de pago son controles independientes.
No se requiere base de datos ni almacenamiento persistente. Las variables y secret files
están documentados para servicios, sin requisito de plan pagado señalado:
[configuración de secretos](https://render.com/docs/configure-environment-variables).

## GitHub

Preparar repositorio PRIVADO en GitHub Free, sin Actions/Packages ni servicios de pago.
Git LFS incluye 10 GiB de almacenamiento y 10 GiB de descarga para Free, pero los excesos
pueden ser facturables según configuración. No activar LFS pagado ni aportar tarjeta.
Los recursos grandes actuales están excluidos por licencia, por lo que no se habilitó LFS.
Si se autoriza incorporarlos, revisar cuotas y presupuesto de cero con bloqueo de uso antes.
[Condiciones oficiales LFS](https://docs.github.com/en/billing/concepts/product-billing/git-lfs).

## Google Drive / Google Cloud

No activar facturación ni pedir aumentos de cuota. El uso estándar de Drive API se describe
sin coste adicional, pero la documentación anuncia cambios y posibles cargos por exceso
durante 2026. Hay cuotas distintas según antigüedad del proyecto y un umbral diario nuevo.
No conocemos el estado de facturación/cuotas de TU cuenta porque no accedimos a ese panel.
Antes de desplegar, confirmar proyecto sin cuenta de facturación asociada y límites de cuota
que rechacen exceso. Si se exige facturación o tarjeta, detener despliegue y revisar opciones.
[Límites oficiales](https://developers.google.com/workspace/drive/api/guides/limits).

Los videos siguen ocupando almacenamiento en la cuenta Drive del profesor, compartido con
otros productos de su cuenta. No se contrató almacenamiento extra. Compartir solo la carpeta
necesaria con la cuenta de servicio; no publicar los archivos por enlace para eludir cuotas.

## Condición previa a publicar

Confirmar por separado: derechos de recursos; autenticación/política pública; GitHub privado;
ninguna tarjeta ni facturación activada; cuotas reales de las cuentas; aceptación de la posible
suspensión del proxy gratuito. Si alguna condición falla, no desplegar ni pagar para corregirla.

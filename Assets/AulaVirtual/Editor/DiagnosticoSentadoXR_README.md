# Diagnóstico de sentado XR — solo Editor y solo lectura

No requiere regenerar interfaces ni añadir componentes a la escena. No modifica scripts existentes, transforms, asientos, panel, simulador, tracking ni eventos de entrada. No guarda la escena. Cerrar la ventana desconecta sus callbacks.

## Semántica comprobada

`PruebaAsientoXR.Sentarse()` orienta el rig con `vistaSentado.forward` y llama a `xrOrigin.MoveCameraToWorldLocation(vistaSentado.position)`. La implementación instalada de XROrigin desplaza el Origin para colocar la cámara en ese destino compensando su pose actual. Por tanto, VistaSentado es el destino de OJOS según el código; PuntoAsiento es su referencia padre en la escena. Esto no demuestra que el marcador se haya colocado a la altura anatómica correcta sobre el modelo de silla.

Un Camera Offset no nulo no es por sí solo un error: el método de Core Utils debe compensarlo en el instante de traslado. Una pose de tracking recibida después puede cambiar la posición local de cámara. PanelPersonalNavyGlass solo coloca su propio transform y no cambia el rig ni la cámara.

## Prueba

1. Abre **Aula Virtual > Diagnóstico > Sentado XR (solo lectura)**.
2. Asigna GestorAsientosXR / PruebaAsientoXR; si hay uno solo se resuelve al armar. Pizarrón es opcional y se busca por nombre en la misma escena.
3. Pulsa **Armar próximo sentado** antes de sentarte. Puedes hacerlo antes o durante Play. Si ya estás sentado, levántate primero.
4. Siéntate con el selector normal o B. Durante unos 1,2 segundos no muevas ratón, mandos, cabeza ni teclas del simulador. No cambies modo Mouse/XR durante la captura. Espera a que salga un informe `[DIAG-SENTADO]`.
5. Pulsa **Copiar último informe** y comparte el texto completo. Repite una segunda prueba sin cerrar Play: levantarse y sentarse. Indica si usaste B o selector y qué modo del simulador estaba activo.
6. Opcional: activa Referencias en Scene y el botón Gizmos de Scene. Amarillo = Vista/destino según código; cian = ojos actuales; flechas = orientación horizontal; línea punteada = diferencia; cubo gris = referencia padre. Se dibujan sobre la escena para poder verlos detrás de geometría.

**Medir ahora** no ejecuta sentado ni modifica nada: solo captura el estado actual, que puede ser de pie. No equivale a capturar el instante de la operación.

## Contenido del informe

- Cámara real usada por el asiento frente a Camera.main, rutas completas, posiciones globales y locales, escalas, componentes de la cadena cámara → padres.
- XROrigin, Origin base, Camera Floor Offset y modo de tracking solicitado/actual, CameraYOffset.
- Última muestra previa de pie (sondeo 10 Hz), muestra inicial al recibir el log existente Estudiante sentado, dos callbacks onBeforeRender y muestras posteriores aproximadamente a 0,1/0,5/1 s. El tiempo real se incluye: un Editor bloqueado puede entregar muestras más tarde.
- Delta = cámara menos Vista en ejes GLOBALes, distancia total y horizontal. Ángulo horizontal cámara→Vista.forward, cámara→centro del pizarrón y Vista.forward→centro del pizarrón. No se comparan pitch/roll como error de orientación horizontal. Si se mira verticalmente, el ángulo puede ser NaN por proyección horizontal indefinida.
- Cambios posteriores de Origin, rotación, Camera Offset local y pose local de cámara; pose leída por la acción del TrackedPoseDriver y control activo si están disponibles. No habilita ni modifica esas acciones.
- Simuladores y componentes de pose/locomoción/transformación presentes y activos. Estar presente o activo no demuestra causalidad. Las conclusiones son orientativas.

La captura inicial usa el log que YA existe al final de Sentarse, sin instrumentar ni modificar esa función. Si ese log deja de existir, la transición se detecta por sondeo y se indica como inicial aproximada. El orden de callbacks onBeforeRender respecto a otros scripts no se fuerza. Solo se publica un informe por prueba, no mensajes cada frame; la ventana debe permanecer abierta.

## Interpretación

- Inicial cercano a cero y estable: la cámara llega al destino que pide el código. Si visualmente el asiento sigue mal, revisar después la colocación del marcador, sin compensarla todavía.
- Inicial correcto y diferencia posterior con Origin estable y pose local variable: compatible con tracking/TrackedPoseDriver. Comparar la acción de posición y su control activo (simulado o físico) antes de atribuirlo al simulador.
- Origin o Camera Offset local cambian después: investigar los participantes activos y cambios de modo/altura. Un movimiento del usuario también puede explicar diferencias.
- Cámara alineada con Vista.forward pero Vista.forward no orientada hacia el pizarrón: el algoritmo sigue el marcador; investigar su orientación prevista y la ubicación del pizarrón.
- Diferencia inicial significativa: revisar referencias, jerarquía y escalas antes de ajustar puntos. Se usa por defecto 1 cm y 2° solo como umbrales de lectura, no como calibración automática.

Comparte el informe completo de al menos dos pruebas, el método de sentado, si moviste dispositivos y una captura de Scene con Gizmos. Para conclusiones sobre tracking físico será necesaria posteriormente una medición equivalente en dispositivo; esta herramienta está excluida de APK por estar en Editor.

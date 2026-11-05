# Aula Virtual Inclusiva

Aplicación educativa de realidad virtual en Unity para acceder a un aula tridimensional,
contenidos audiovisuales de Google Drive, bibliotecas XR y controles personales desde el asiento.
La accesibilidad es un objetivo de diseño; las pruebas con usuarios y Quest siguen su plan propio.

**Raíz única de trabajo: `C:/U/Aula_Virtual_Inclusiva`.** Unity permanece directamente aquí.
El repositorio GitHub previsto se llama `Aula-Virtual-Inclusiva` y debe ser privado.
No se publicó, no se desplegó Render y no se generó APK durante esta reorganización.

```text
Aula_Virtual_Inclusiva/
  Assets/             Escena, scripts, materiales y recursos Unity
  Packages/           Dependencias fijadas de Unity
  ProjectSettings/    Configuración existente XR/URP/Android
  backend/            Express y Google Drive API
  docs/               Guías, licencias, auditorías y resultados
  tools/              Comprobación de Unity/Git y restauración legal de recursos
  README.md
  .gitignore
  .gitattributes
  render.yaml
```

## Trabajar desde ahora

Abrir **esta misma carpeta original** en Unity Hub con Unity **6000.6.3f1**. La escena es
`Assets/AulaVirtual/Scenes/AulaPrincipal_GLB.unity`. Guardar normalmente los cambios deseados
para que aparezcan en Git. No hay que copiar el salón a otra carpeta ni sincronizar proyectos.
`PreparacionGitHub/` permanece como archivo histórico ignorado; ya no contiene un Git activo.
`C:/U/AulaVirtualBackend` también se conserva como referencia; el backend activo es `./backend`.
El respaldo previo externo está documentado en [ORGANIZACION.md](docs/ORGANIZACION.md).

## Arquitectura y tecnologías

Unity consulta un backend Node/Express que accede a una carpeta autorizada de Drive mediante
cuenta de servicio y scope de solo lectura. Transmite los MP4 sin almacenarlos en Render.
Unity mantiene el último catálogo válido y videos versionados en `persistentDataPath`; ambas
bibliotecas reutilizan el catálogo/caché y el VideoPlayer con RT_VideoAula existentes.
El cine conserva su entrada independiente. Las credenciales no forman parte de Unity ni APK.

Unity 6.6, URP, XRI 3.6.1, XR Hands 1.8.1, OpenXR, Input System y TextMeshPro; backend con
Node 24 LTS, Express 5 y googleapis. Se conservan las configuraciones del salón original,
incluidos perfiles Meta Quest que estaban desactivados en la preparación anterior.
La aparición de manos simuladas no equivale a validar seguimiento real o rendimiento en Quest.

## Iniciar el backend

Con Node 24 LTS y las credenciales locales autorizadas configuradas fuera del repositorio:

```powershell
cd C:/U/Aula_Virtual_Inclusiva/backend
npm ci
# Si hace falta, copiar .env.example a .env y completar carpeta/ruta privada del JSON.
npm start
```

No sobrescribir un .env existente. Ver [backend/README.md](backend/README.md).
La configuración pública única de Unity sigue en
`Assets/AulaVirtual/Resources/BibliotecaBackend.json`. Editor usa Remoto por defecto:
https://aula-virtual-inclusiva.onrender.com. Se puede seleccionar Local para desarrollo;
Android utiliza siempre la URL remota HTTPS. Ver [CONEXION_RENDER.md](docs/CONEXION_RENDER.md).

## Verificar y contribuir

Desde la raíz ejecutar `python tools/auditar.py` antes de preparar un commit.
Desde backend, `npm test`. Con Unity cerrado, `./tools/validar-unity.ps1` compila y abre la
escena en modo batch para inspección estructural, sin guardarla ni generar APK. Preserva
los assets que Unity normaliza al cerrar. La comprobación visual y Play Mode son adicionales.

Git local usa `main`, sin remoto ni commits automáticos. Los cambios de Assets/ se registran
directamente desde este proyecto. Ver [PUBLICACION.md](docs/PUBLICACION.md) antes de hacer
commit o push. Unity/Node generan Library y node_modules localmente; quedan fuera de Git.

## Licencias, secretos y servicio gratuito

Los modelos CGTrader, texturas de procedencia pendiente y videos se conservan localmente
pero se excluyen de Git. Un clon requiere restauración legal para reproducir todo el aula:
[LICENCIAS.md](docs/LICENCIAS.md). No se activa LFS ni almacenamiento adicional.
Las claves/JSON privados, .env, keystores, APK y respaldos están excluidos. La auditoría es
heurística y se complementa con revisión manual: [SEGURIDAD.md](docs/SEGURIDAD.md).

El propietario publicó el backend en Render Free, con raíz backend, npm ci y npm start.
El código conserva su bloqueo de producción salvo autorización explícita de material público;
esta conexión de Unity no cambia autenticación, secretos ni configuración del servicio.
No agregar tarjeta ni activar facturación. Revisar [GRATUIDAD.md](docs/GRATUIDAD.md) y cuotas
reales antes de desplegar; no se promete disponibilidad gratuita ilimitada.

[Guía Unity y APK](docs/UNITY.md) · [Resultados y pendientes](docs/VALIDACION.md) ·
[Reorganización definitiva](docs/ORGANIZACION.md)

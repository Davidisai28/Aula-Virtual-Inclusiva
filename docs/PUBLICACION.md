# GitHub y Render: publicación pendiente

La raíz Git definitiva es C:/U/Aula_Virtual_Inclusiva. Rama principal main. No hay remoto,
commits ni publicación automática. El Git de PreparacionGitHub, que no tenía commits,
se archivó externamente; esa carpeta queda ignorada y no es un repositorio anidado activo.

## Revisar desde el salón original

```powershell
cd C:/U/Aula_Virtual_Inclusiva
git status --short
python tools/auditar.py
git check-ignore -v Library/prueba backend/.env backend/node_modules/prueba PreparacionGitHub/prueba
```

Comprobar docs/archivos-incluidos.txt, auditoria-git.json y licencias. Guardar voluntariamente
los cambios de Unity que se quieran registrar. No se guardan escenas por esta herramienta.

Solo tras revisar y autorizar el primer commit:

```powershell
git add Assets Packages ProjectSettings backend docs tools README.md .gitignore .gitattributes render.yaml
git diff --cached --stat
git diff --cached --check
git commit -m "Organizar Aula Virtual Inclusiva en su raiz original"
```

No usar -f para incluir modelos restringidos, credenciales o carpetas generadas. No es necesario
sincronizar otra copia de Unity. Estos comandos de commit NO se ejecutaron durante la preparación.

## Repositorio privado, solo después de autorización

Crear Aula-Virtual-Inclusiva en GitHub Free con visibilidad Private, sin otro README inicial,
sin servicios de pago ni tarjeta. Usar la URL real del repositorio y autenticación segura del sistema:

```powershell
git remote add origin URL_REAL_DEL_REPOSITORIO_PRIVADO
git push -u origin main
```

No insertar tokens en la URL. Confirmar colaboradores y sus licencias de recursos. Los modelos
CGTrader y texturas excluidos permanecen locales: un clon no los recibe automáticamente.

## Desarrollo posterior

```powershell
git switch -c mejora/nombre-descriptivo
# Editar el mismo salón original o backend/; ejecutar pruebas pertinentes
python tools/auditar.py
git add RUTAS_REVISADAS
git commit -m "Describir cambio"
# Publicar solo cuando se autorice:
git push -u origin mejora/nombre-descriptivo
```

## Render, autorización separada

Repositorio Aula-Virtual-Inclusiva, rama main, Root Directory backend, Build npm ci,
Start npm start, health /health, Node 24 LTS, instancia Free. render.yaml conserva filtros
backend/** y autodeploy deshabilitado. No crear base de datos, disco persistente ni cron.
Configurar el JSON como secreto privado fuera de Git y resolver acceso a videos antes de
habilitar producción. Revisar GRATUIDAD y SEGURIDAD: no habilitar facturación ni añadir tarjeta.
Si el alta exige un medio de pago, detenerse. Después de despliegue autorizado, introducir
la URL HTTPS real en Assets/AulaVirtual/Resources/BibliotecaBackend.json y probar Editor remoto.
La APK tendrá una autorización posterior; esta reorganización no crea builds.

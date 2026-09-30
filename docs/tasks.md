# Backlog inmediato

## Fase 1 · Núcleo local

Orden sugerido para cambios pequeños y completos:

1. **Hecho: preparar solución y herramienta de desarrollo.** SDK .NET 10 local, solución, bibliotecas `Core` y `Storage`, xUnit, `.gitignore` e instrucciones de desarrollo. No afecta a la instalación final de usuario.
2. **Hecho: migración SQLite 001.** `providers`, `projects`, `conversations`, `messages`, `message_revisions` y `capture_state`, con claves únicas e índices. Base bajo `%LOCALAPPDATA%` por defecto y versión en `PRAGMA user_version`.
3. **Hecho: contrato de observación.** DTO normalizado independiente de ChatGPT y validación de tamaño, rol, claves e identificador de conversación. Los datos sintéticos de las pruebas sirven como origen local; aún sin extensión.
4. **Hecho: ingestión transaccional.** El mismo lote no duplica contenido; añadir 10 mensajes después de 120 añade solo 10. Los cambios de contenido, rol u orden guardan una revisión. Un lote inválido no escribe mensajes parciales.
5. **Hecho: consultas mínimas de diagnóstico.** Lectura de conversaciones, mensajes y última captura/error desde el núcleo. La utilidad de desarrollo queda opcional.
6. **Hecho: prueba real limitada de Chrome antes del adaptador completo.** La [prueba con chats ficticios](chatgpt-capture-spike.md) verificó el enlace extensión–host, chat nuevo, continuación, edición, regeneración, recarga, reinicio de Chrome, proyecto, navegación entre dos chats y un adjunto de texto ficticio. Encontró roles no identificables en el DOM actual, nodos ocultos durante navegación y contadores transitorios. Después se redujo el protocolo a IDs y metadatos, se eliminaron roles inferidos y se separó la comparación por pestaña; estos cambios pasaron pruebas locales ficticias, pero aún no una nueva prueba en Chrome. No se demostró captura completa ni automática. Chats largos, ramas históricas y recuperación de fallos pasan a la puerta de viabilidad de Fase 3.

## Criterios de aceptación de Fase 1

- El usuario final todavía no necesita instalar nada: esta fase es interna.
- La base se crea en la ruta local prevista y se migra sin perder datos.
- Dos entregas idénticas dejan una sola conversación y un solo mensaje por clave.
- Una entrega con 10 mensajes nuevos añade exactamente 10, y no dispara trabajo posterior para los 120 anteriores.
- Una edición conserva la versión previa y queda trazable.
- Las pruebas ejecutan sin red ni cuenta de OpenAI.
- `architecture.md`, `decisions.md` y este backlog se actualizan si la prueba cambia supuestos.

## Riesgos a cerrar pronto

1. **Cobertura real de ChatGPT:** la extensión encontró IDs en los mensajes visibles de los chats ficticios, pero no pudo determinar sus roles; el DOM también conservó nodos ocultos al navegar. Ediciones y regeneraciones crearon IDs nuevos. El modelo debe representar roles desconocidos, ramas y cobertura antes de captar datos reales.
2. **Proyectos:** se observó el formato de ruta `g-p-` seguido de 32 caracteres hexadecimales y el host lo confirmó en chats ficticios. Se guardará asociación solo cuando haya evidencia; otros formatos no están verificados.
3. **Instalación:** extensión y host deben enlazarse sin terminal; IDs de tienda y firma pueden complicar el flujo.
4. **Privacidad:** la base inicial no tiene cifrado de aplicación; explicar protección real, exportación legible y borrado.
5. **Entorno de desarrollo:** el SDK .NET 10 ya está instalado localmente en `.tools`; Inno Setup se necesitará al empaquetar, pero nunca en la máquina de los usuarios.
6. **Repositorio público:** no subir datos reales, archivos `.env`, bases locales ni exportaciones; revisar archivos y metadatos de autor antes de publicar nuevos commits.

## Próxima puerta de viabilidad

Antes de una captura personal o automática: identificar roles con evidencia, representar rama activa y versiones ocultas, medir chats largos y mensajes fuera de pantalla, mostrar cobertura y errores, y probar recuperación de entregas. El transporte local funciona; estas condiciones siguen pendientes.

## Fase 2.1 · Desktop mínimo

- [x] Añadir `PersonalContext.Desktop` a `PersonalContext.sln`: WPF, WinExe, .NET 10 Windows y referencia a Storage.
- [x] Abrir LocalStore y contar conversaciones fuera del hilo de interfaz, tras cargar la ventana.
- [x] Mostrar carga, estado, ruta, contador o error comprensible, sin borrar ni sustituir la base.
- [x] Mostrar que la captura de ChatGPT todavía no está conectada; mantener el prototipo aislado y el esquema sin cambios.
- [x] Documentar restore, test y publish con el SDK Windows desde Bash en WSL y conservar PowerShell.
- [x] Smoke manual confirmado por el usuario en la cuenta local estándar de prueba `PCH-Smoke`: ventana sin consola y con respuesta normal, base bajo el perfil de prueba, contadores 0/2 y reapertura; detalle abajo, separado de las comprobaciones automáticas.
- [x] Smoke manual confirmado por el usuario con `publish-clean` copiado a una ruta con espacios; fixture SQLite inválida con mensaje comprensible y contador «No disponible»; restauración válida con contador 2.
- [ ] Verificar la carpeta completa en una máquina Windows x64 sin .NET instalado; esta condición no queda acreditada por el smoke confirmado.
- [ ] Fase 2 posterior: instalador por usuario, registro automático del puente y prueba de instalación. **Fase 2 permanece pendiente.**

## Comprobaciones automáticas locales de Fase 2.1 · 30 de septiembre de 2026

SDK Windows 10.0.401 ejecutado desde Bash en WSL con `./.tools/dotnet/dotnet.exe`:

- [x] Restore de `PersonalContext.sln` con `NuGet.Config`, código de salida 0.
- [x] Build de Desktop en Release con `--no-restore`, sin errores ni advertencias.
- [x] Pruebas existentes: 12 correctas, 0 fallidas y 0 omitidas; repetición autorizada con `--no-build --no-restore` tras no poder recuperar el resultado de la sesión interrumpida. Informe en `.tools/test-results/phase-2.1.trx`.
- [x] Publish local `win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false` en `.tools/desktop/publish/`, código de salida 0. Incluye runtime .NET/WPF 10.0.12 y SQLite nativo; ejecutable PE x64 con subsistema gráfico.
- [x] Finales de línea conservados. `git diff --check` señala CRLF de los cuatro archivos ya modificados al empezar, incluidas las nuevas líneas CRLF de la solución; `git -c core.whitespace=cr-at-eol diff --check` pasa sin avisos.

No se ejecutó Desktop ni se accedió a la base del perfil habitual. Las pruebas de Storage usan bases temporales y datos ficticios. No se repitieron los smoke del prototipo, ya que NativeHost y la extensión no cambiaron. La compilación y la publicación no sustituyen el smoke manual: su confirmación posterior por el usuario se registra en una sección independiente.

## Corrección de privacidad del paquete · Fase 2.1

El SDK instalado 10.0.401 separa el PDB de salida propio de los símbolos de las referencias. Desktop usa `CopyOutputSymbolsToPublishDirectory=false` y filtra los PDB de `ResolvedFileToPublish` tras `ComputeFilesToPublish`, conservando los símbolos de build. La primera corrección retiró 3 PDB, pero la inspección encontró rutas del perfil en `PersonalContext.Core.dll`, `PersonalContext.Desktop.dll` y `PersonalContext.Storage.dll`; esa salida intermedia tampoco es distribuible.

Publicación final en una carpeta nueva y vacía, con propiedad global `PathMap` para Desktop y sus referencias:

```bash
projectSourceRoot=$(wslpath -w "$PWD")
./.tools/dotnet/dotnet.exe publish src/PersonalContext.Desktop/PersonalContext.Desktop.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=false -p:PublishTrimmed=false "-p:PathMap=$projectSourceRoot=/_/" -o .tools/desktop/publish-clean
```

- Publish: código de salida 0, sin errores ni advertencias. Log local en `.tools/desktop/phase-2.1-publish-clean.log` (no distribuir).
- Inventario final: 406 archivos, 148.633.359 bytes; 402 DLL, 2 EXE y 2 JSON, 0 PDB. Runtime .NET/WPF y SQLite nativo presentes. Inventario con tamaños y SHA-256 en `.tools/desktop/phase-2.1-package-audit.json`, fuera del paquete.
- Inspección del contenido de todos los archivos en ASCII/UTF-8 y UTF-16: 0 archivos con rutas absolutas de perfil Windows/WSL. Las 3 entradas CodeView de las DLL propias apuntan a rutas mapeadas; no hay PDB incrustados en ellas.
- PDB de Core, Storage y Desktop conservados en las salidas de build.
- Las carpetas anteriores `.tools/desktop/publish/` y `.tools/desktop/publish-no-symbols/` no son distribuibles. Solo `.tools/desktop/publish-clean/` es el candidato actual.

No se repitieron restore ni pruebas: la corrección afecta al empaquetado y al mapeo de rutas de depuración; se validó mediante publish e inspección de artefactos. No se ejecutó Desktop ni se realizó smoke visual, commit o push. Ese registro corresponde a las comprobaciones automáticas de empaquetado anteriores al smoke confirmado por el usuario. Sigue pendiente la prueba en una máquina sin .NET instalado; esta revisión del paquete no es una auditoría exhaustiva de secretos o del historial.

## Smoke manual de Fase 2.1 · Confirmación del usuario

El usuario confirmó el siguiente smoke en Windows 11 Home 25H2, usando la cuenta local estándar de prueba `PCH-Smoke` y la carpeta completa `.tools/desktop/publish-clean/` copiada a una ruta con espacios. Es una **observación manual comunicada por el usuario**, no una comprobación automática ni una ejecución del agente.

- Ventana sin consola y con respuesta normal.
- Base creada bajo el perfil de prueba; contador inicial 0 y reapertura con 0.
- Fixture ficticia válida con 2 conversaciones; contador 2 y reapertura con 2.
- Fixture SQLite inválida: mensaje comprensible y contador «No disponible».
- Restauración de la base ficticia válida: contador 2.

La confirmación no acredita una máquina sin .NET instalado: esa prueba sigue pendiente. El instalador por usuario, el registro automático del puente y la prueba de instalación también siguen pendientes; **Fase 2 no está completada**. La captura experimental continúa aislada de SQLite.

Esta actualización solo documenta la confirmación: no se accedió a la base del perfil habitual ni a datos de `PCH-Smoke`, y no se repitieron build, test ni publish.

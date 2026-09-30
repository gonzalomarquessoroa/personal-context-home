# Personal Context Home

Memoria personal local y reutilizable a partir de conversaciones con asistentes de IA. La primera versión se centrará en **ChatGPT usado en un navegador de Windows**. El objetivo de producto es instalar una vez, configurar en menos de 15 minutos y recibir cambios nuevos sin tareas manuales recurrentes.

## Estado

**Fase 1 completada: núcleo local probado y enlace real Chrome–host verificado con chats ficticios.** La extensión experimental identifica mensajes visibles y proyectos, pero la mayoría de roles no se distinguen en el DOM actual. El prototipo actual envía solo IDs y metadatos mínimos al host, sin texto de mensajes. La Fase 2.1 añade una ventana WPF mínima que abre LocalStore en segundo plano y muestra estado, ruta y número de conversaciones, o un error comprensible. La captura de ChatGPT todavía no está conectada. Fase 2 sigue pendiente: faltan la prueba en una máquina sin .NET instalado, el instalador y el registro automático del puente. El usuario confirmó el smoke manual de Fase 2.1 en una cuenta local estándar de prueba; se documenta por separado de las comprobaciones automáticas en [tasks.md](docs/tasks.md#smoke-manual-de-fase-21--confirmación-del-usuario). La [arquitectura](docs/architecture.md) distingue el estado actual del diseño previsto.

La [prueba real de Chrome](docs/chatgpt-capture-spike.md) comprobó el puente y el observador con conversaciones ficticias: continuación, edición, regeneración, recarga, proyectos y navegación entre chats. Las ediciones y regeneraciones crean versiones con IDs distintos. La cobertura de ramas y mensajes fuera de pantalla sigue sin verificarse.

## Decisión técnica inicial

- Aplicación Windows en C# con .NET 10 y WPF; lógica y almacenamiento separados de la interfaz.
- SQLite en el perfil local del usuario, sin servidor ni cuenta.
- Instalador Windows por usuario, con binarios .NET autocontenidos. Inno Setup es el candidato inicial porque también debe registrar el puente con la extensión del navegador.
- Extensión Chromium Manifest V3 con acceso limitado a ChatGPT y comunicación con un proceso local mediante Native Messaging. El enlace funciona en la prueba; la **viabilidad de captura completa sigue condicionada** por los roles, ramas y cobertura del DOM.
- Sin API de modelos ni suscripción adicional en las primeras fases. La extracción de memoria se evaluará después de disponer de datos incrementales fiables.

La documentación de la API de OpenAI describe conversaciones creadas y gestionadas mediante la API; no establece una vía para leer el historial personal de la interfaz de ChatGPT. No basaremos la captura en endpoints privados ni en cookies de sesión. Véase [OpenAI Docs: Conversation state](https://developers.openai.com/api/docs/guides/conversation-state).

## Documentación

- [Alcance y criterio de éxito](docs/project.md)
- [Arquitectura, modelo y estrategia de captura](docs/architecture.md)
- [Decisiones técnicas](docs/decisions.md)
- [Fases y condiciones de avance](docs/roadmap.md)
- [Tareas y validación de Fases 1 y 2.1](docs/tasks.md)

## Próximo paso

Verificar el paquete WPF en una máquina sin .NET instalado y abordar después el instalador y el registro del puente. Resolver las [lagunas de captura](docs/chatgpt-capture-spike.md#resultado-y-limites-de-la-prueba-real) antes de conectar ChatGPT a SQLite.

## Desarrollo

Solo quien desarrolla necesita el SDK .NET 10. El paquete WPF se publica por carpeta con su runtime incluido, sin single-file ni trimming. Para compilar y probar desde PowerShell:

```powershell
dotnet restore PersonalContext.sln --configfile NuGet.Config
dotnet test PersonalContext.sln --no-restore
```

El SDK también puede instalarse de forma local en `.tools/dotnet`, carpeta excluida de Git. Las pruebas usan bases temporales y no requieren cuenta de OpenAI.

El host de prueba se publica con runtime incluido y se comprueba con datos ficticios así:

```powershell
dotnet publish src/PersonalContext.NativeHost/PersonalContext.NativeHost.csproj -c Release -r win-x64 --self-contained true -o .tools/native-host/publish
node scripts/probe-host-smoke.mjs
node scripts/probe-extension-fixture.mjs
node scripts/probe-worker-fixture.mjs
```

La extensión está en `providers/chatgpt/extension/`. Se activa manualmente desde el botón de Chrome solo en la pestaña elegida. El host de prueba no recibe texto, no escribe conversaciones ni alimenta SQLite. Las pruebas locales posteriores al ensayo real usan exclusivamente DOM y datos ficticios; los cambios más recientes aún no se han vuelto a comprobar en Chrome.

## Repositorio público y datos privados

Este código está preparado para publicarse, pero **no** se deben añadir conversaciones reales, bases de datos, exportaciones, claves ni archivos `.env`. `.gitignore` excluye las rutas locales habituales; antes de cada publicación se revisarán los archivos incluidos y el historial de Git. Los commits deben usar la dirección privada `noreply` configurada en GitHub.

## Desktop · Fase 2.1

`src/PersonalContext.Desktop/` es un ejecutable WPF `net10.0-windows` que referencia Storage. Al mostrar la ventana, consulta `LocalStore.GetConversations()` mediante `Task.Run`: inicialización y lectura no bloquean el hilo de la interfaz. Muestra carga, disponibilidad o error, la ruta de la base y el contador (no disponible si falla). No ingiere datos ni conecta el prototipo de ChatGPT. La ruta por defecto es `%LOCALAPPDATA%\PersonalContextHome\data\personal-context.db`; la base no se guarda junto al ejecutable. Ante errores no se borra ni sustituye la base.

Desde Bash en WSL, usando el SDK **de Windows** ya instalado, desde la raíz:

```bash
./.tools/dotnet/dotnet.exe --info
./.tools/dotnet/dotnet.exe restore PersonalContext.sln --configfile NuGet.Config
./.tools/dotnet/dotnet.exe test PersonalContext.sln -c Release --no-restore
projectSourceRoot=$(wslpath -w "$PWD")
./.tools/dotnet/dotnet.exe publish src/PersonalContext.Desktop/PersonalContext.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false "-p:PathMap=$projectSourceRoot=/_/" -o .tools/desktop/publish-clean
```

Comprueba el código de salida de cada comando y detente ante fallos. Estas rutas relativas sirven al SDK de Windows; no uses el `dotnet` de Linux para esta validación. En PowerShell, selecciona `$projectDotnet = ".\.tools\dotnet\dotnet.exe"` y `$projectSourceRoot = (Get-Location).Path`; usa `& $projectDotnet` con los mismos argumentos, incluido `"-p:PathMap=$projectSourceRoot=/_/"`. Los comandos completos están en [AGENTS.md](AGENTS.md) y el procedimiento de publicación en [.agents/skills/dotnet-wpf-build/SKILL.md](.agents/skills/dotnet-wpf-build/SKILL.md).

El proyecto excluye los PDB solo del publish (`CopyOutputSymbolsToPublishDirectory=false` y filtro final de símbolos de las referencias); siguen disponibles en `bin`/`obj` para desarrollo y pruebas. El comando de distribución añade `PathMap` como propiedad global para sanear también las rutas de depuración de las DLL de Desktop, Core y Storage. Una compilación de desarrollo normal no aplica ese mapeo. Usa una salida nueva y vacía en cada publicación: publish no elimina archivos antiguos. Comprueba que el paquete no contiene PDB ni rutas personales antes de distribuirlo. La carpeta anterior `.tools/desktop/publish/` contiene símbolos con rutas personales y **no es distribuible**. La salida intermedia `.tools/desktop/publish-no-symbols/` tampoco lo es: aún conserva rutas en tres DLL.

Copia **toda** `.tools/desktop/publish-clean/` a Windows x64. El ejecutable es `PersonalContext.Desktop.exe`. El usuario confirmó el smoke manual en Windows 11 Home 25H2, cuenta local estándar `PCH-Smoke`, con el paquete copiado a una ruta con espacios: ventana sin consola y con respuesta normal, base bajo el perfil de prueba, contadores 0 y 2 conservados al reabrir, error comprensible y contador «No disponible» con una fixture SQLite inválida, y contador 2 al restaurar la fixture válida. Esta observación del usuario es distinta de las comprobaciones automáticas; véase el [registro del smoke](docs/tasks.md#smoke-manual-de-fase-21--confirmación-del-usuario). Sigue pendiente ejecutar el paquete en una máquina sin .NET instalado, usando solo un perfil o VM desechable y datos ficticios. No ejecutes esta comprobación contra la base del perfil Windows habitual. Compilar y publicar desde WSL con el SDK Windows no acredita el arranque visual ni la carga de SQLite en la máquina destino.

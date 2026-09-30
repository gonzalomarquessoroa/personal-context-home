# Personal Context Home

Memoria personal local y reutilizable a partir de conversaciones con asistentes de IA. La primera versión se centrará en **ChatGPT usado en un navegador de Windows**. El objetivo de producto es instalar una vez, configurar en menos de 15 minutos y recibir cambios nuevos sin tareas manuales recurrentes.

## Estado

**Fase 1 completada: núcleo local probado y enlace real Chrome–host verificado con chats ficticios.** La extensión experimental identifica mensajes visibles y proyectos, pero la mayoría de roles no se distinguen en el DOM actual. El prototipo actual envía solo IDs y metadatos mínimos al host, sin texto de mensajes. No existe todavía aplicación de escritorio ni captura apta para datos personales. La [arquitectura](docs/architecture.md) distingue el estado actual del diseño previsto.

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
- [Backlog inmediato de Fase 1](docs/tasks.md)

## Próximo paso

Resolver las [lagunas detectadas en la prueba](docs/chatgpt-capture-spike.md#resultado-y-limites-de-la-prueba-real) antes de conectar el adaptador de ChatGPT a SQLite.

## Desarrollo

Solo quien desarrolla necesita el SDK .NET 10. La aplicación distribuida se publicará con su runtime incluido. Para compilar y probar desde PowerShell:

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

# Personal Context Home

Memoria personal local y reutilizable a partir de conversaciones con asistentes de IA. La primera versión se centrará en **ChatGPT usado en un navegador de Windows**. El objetivo de producto es instalar una vez, configurar en menos de 15 minutos y recibir cambios nuevos sin tareas manuales recurrentes.

## Estado

**Fase 1 en curso: núcleo local implementado y probado; todavía no hay aplicación de escritorio ni captura de ChatGPT.** La propuesta de captura necesita una prueba real antes de prometer sincronización automática fiable. La [arquitectura](docs/architecture.md) distingue el estado actual del diseño previsto.

## Decisión técnica inicial

- Aplicación Windows en C# con .NET 10 y WPF; lógica y almacenamiento separados de la interfaz.
- SQLite en el perfil local del usuario, sin servidor ni cuenta.
- Instalador Windows por usuario, con binarios .NET autocontenidos. Inno Setup es el candidato inicial porque también debe registrar el puente con la extensión del navegador.
- Extensión Chromium Manifest V3 con acceso limitado a ChatGPT y comunicación con un proceso local mediante Native Messaging. Es una **hipótesis de implementación**, pendiente de prueba con chats nuevos, continuaciones, ediciones y proyectos.
- Sin API de modelos ni suscripción adicional en las primeras fases. La extracción de memoria se evaluará después de disponer de datos incrementales fiables.

La documentación de la API de OpenAI describe conversaciones creadas y gestionadas mediante la API; no establece una vía para leer el historial personal de la interfaz de ChatGPT. No basaremos la captura en endpoints privados ni en cookies de sesión. Véase [OpenAI Docs: Conversation state](https://developers.openai.com/api/docs/guides/conversation-state).

## Documentación

- [Alcance y criterio de éxito](docs/project.md)
- [Arquitectura, modelo y estrategia de captura](docs/architecture.md)
- [Decisiones técnicas](docs/decisions.md)
- [Fases y condiciones de avance](docs/roadmap.md)
- [Backlog inmediato de Fase 1](docs/tasks.md)

## Próximo paso

Realizar una prueba corta con la extensión y el puente local para validar la cobertura real de la captura, antes de construir el adaptador de ChatGPT.

## Desarrollo

Solo quien desarrolla necesita el SDK .NET 10. La aplicación distribuida se publicará con su runtime incluido. Para compilar y probar desde PowerShell:

```powershell
dotnet restore PersonalContext.sln --configfile NuGet.Config
dotnet test PersonalContext.sln --no-restore
```

El SDK también puede instalarse de forma local en `.tools/dotnet`, carpeta excluida de Git. Las pruebas usan bases temporales y no requieren cuenta de OpenAI.

## Repositorio público y datos privados

Este código está preparado para publicarse, pero **no** se deben añadir conversaciones reales, bases de datos, exportaciones, claves ni archivos `.env`. `.gitignore` excluye las rutas locales habituales; antes de cada publicación se revisarán los archivos incluidos y el historial de Git. Los commits deben usar la dirección privada `noreply` configurada en GitHub.

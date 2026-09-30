# Registro de decisiones

Decisiones propuestas en Fase 0, 29 de septiembre de 2026. Las sujetas a prueba se revisarán antes de construir sobre ellas.

## D-001 · .NET 10 para el núcleo y WPF para la interfaz

La primera distribución será solo Windows. WPF permite una interfaz nativa sencilla con una sola plataforma y sin runtime web o Node instalado por el usuario. .NET 10 tiene soporte hasta noviembre de 2028 según [Microsoft Lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core). El núcleo ya es independiente de WPF para facilitar pruebas. El SDK .NET 10 se instaló localmente para el desarrollo; no se implementa interfaz en Fase 1.

## D-002 · SQLite local sin ORM (implementada)

Una base por usuario evita servidor y credenciales. `Microsoft.Data.Sqlite` proporciona acceso directo y ligero. Usaremos SQL explícito y migraciones pequeñas; introducir un ORM solo si reduce complejidad real. La base reside en el perfil local, no se cifra en la primera fase. [Referencia de Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/).

## D-003 · Captura web con extensión MV3 + Native Messaging (condicional)

El historial de ChatGPT usado en la web no aparece documentado como recurso de la API de OpenAI. La [Conversations API](https://developers.openai.com/api/docs/guides/conversation-state) gestiona conversaciones de aplicaciones que usan la API. **Inferencia:** no es una vía soportada para sincronizar el historial personal de ChatGPT. Además, las [guías de plugins de OpenAI](https://developers.openai.com/plugins/app-guidelines) prohíben que un servidor MCP reconstruya todo el historial desde el cliente; un plugin no resuelve este caso.

Una extensión puede observar la página con consentimiento y enviar observaciones a un host local mediante [Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging). Se evita la dependencia de APIs privadas, cookies y un servidor local expuesto. La [prueba real](chatgpt-capture-spike.md) confirmó el transporte Chrome–host y los IDs visibles de chats ficticios, pero no roles fiables ni cobertura completa. Sigue existiendo dependencia del HTML de ChatGPT: se acepta para producción solo si se demuestra cobertura suficiente, errores visibles y mantenimiento razonable. No se prometerá captura de app de escritorio o móvil.

**Puerta de decisión:** conversaciones nuevas, continuación, edición, regeneración, proyectos y reinicio del navegador ya se probaron con datos ficticios. Faltan roles verificables, ramas históricas, chats largos, cobertura fuera de pantalla y recuperación de fallos. Hasta entonces no se conecta el adaptador a datos personales. Si fallan identidad o detección de forma sistemática, se replanteará el alcance; la importación manual no se convertirá por defecto en sincronización periódica.

## D-004 · Publicación autocontenida e instalador por usuario (propuesta)

Publicar `win-x64 --self-contained` evita instalar .NET al usuario. Un instalador Inno Setup es candidato porque debe copiar archivos y registrar el host Native Messaging en HKCU sin pedir administración. [Microsoft](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path) recoge esta opción y advierte que las actualizaciones requieren trabajo propio. Primero se verificará la instalación en una máquina limpia. La extensión probablemente requerirá una instalación consciente desde la tienda del navegador; el instalador deberá guiar y verificar la conexión. Las advertencias SmartScreen y la firma de código afectan la prueba con usuarios no técnicos.

## D-005 · Sin LLM en el núcleo inicial (decidido)

Ingestión, deduplicación exacta, persistencia, búsqueda textual y exportación no necesitan un modelo. Extracción semántica, equivalencias y contradicciones se evaluarán más adelante con lotes pequeños de mensajes nuevos y coste medido. Nunca enviar conversaciones completas a un servicio externo por defecto.

## D-006 · Repositorio público, datos locales fuera de Git (decidido)

El código se publicará en GitHub sin conversaciones, bases de datos, importaciones, exportaciones, SDK local ni secretos. `.gitignore` excluye estas rutas habituales, pero no sustituye la revisión de archivos y del historial antes del primer push. El autor de los commits locales usa la dirección privada `noreply` de GitHub. El primer commit se reescribió antes de crear un remoto para retirar el correo personal de la historia que se publicará. Véase [GitHub Docs: commit email](https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address).

## D-007 · Las versiones de ChatGPT requieren identidad de rama (provisional)

La [prueba exploratoria](chatgpt-capture-spike.md) observó que editar un mensaje o regenerar una respuesta crea un identificador nuevo y retira la versión anterior de la rama visible. El contrato actual de `MessageObservation` sirve para la ingestión idempotente de mensajes conocidos, pero no basta para declarar cuál está activo ni para distinguir una rama oculta de un borrado. No se conectará a datos personales hasta añadir una representación verificable de rama y cobertura. No se usará la posición visual como identidad duradera ni se inferirá borrado a partir de ausencia en el DOM.

## D-008 · Prototipo manual y efímero para medir cobertura

La extensión de Fase 1 se activa con un clic en una pestaña concreta; `activeTab` evita acceso permanente a todo el historial. El host valida lotes y compara conjuntos de IDs visibles por pestaña y conversación **solo en memoria del proceso**, devolviendo contadores sin texto ni IDs. El código actualizado tampoco envía texto al host ni infiere roles por alternancia. Es una herramienta de viabilidad, no el adaptador de producción. La prueba real confirmó el enlace y la asociación a proyecto, pero encontró roles desconocidos y transiciones de DOM que alteraban los acumulados; el recuadro ya no muestra ese acumulado. Los cambios posteriores pasaron pruebas ficticias locales, pero aún no otra prueba real de Chrome. Se mantiene el prototipo aislado de SQLite y no se interpreta ausencia visual como borrado.

## D-009 · Ventana mínima y carpeta autocontenida para Fase 2.1 (implementada; smoke manual confirmado por el usuario, prueba sin .NET pendiente)

Se añade Desktop a la solución como WPF `net10.0-windows` WinExe, con referencia a Storage. La consulta síncrona existente de LocalStore se ejecuta mediante `Task.Run` después de cargar la ventana, sin cambiar Core, Storage ni el esquema SQLite. Muestra ruta, estado y número de conversaciones; los fallos tienen una explicación visible y no provocan borrado o sustitución automática. No se añade ingestión ni conexión a la captura experimental; la ventana lo indica explícitamente.

La primera entrega es una carpeta Windows x64 autocontenida, sin single-file ni trimming, para conservar el runtime WPF y los binarios nativos de SQLite. Desde Bash en WSL se utiliza `./.tools/dotnet/dotnet.exe` (SDK Windows), manteniendo también los comandos PowerShell. El smoke visual se reserva a perfiles/VM desechables con datos ficticios. El usuario confirmó arranque y respuesta normal desde una ruta con espacios en la cuenta local estándar `PCH-Smoke` de Windows 11 Home 25H2, con base bajo ese perfil, reapertura con contadores 0/2, error ante una fixture SQLite inválida y restauración válida con contador 2. Esta observación manual se registra [por separado de las comprobaciones automáticas](tasks.md#smoke-manual-de-fase-21--confirmación-del-usuario); sigue pendiente la prueba en una máquina sin .NET instalado. D-004 sigue pendiente en cuanto al instalador, registro del puente y experiencia de instalación; Fase 2 permanece pendiente.

La corrección de privacidad del paquete excluye los PDB mediante `CopyOutputSymbolsToPublishDirectory=false` y un filtro final de referencias en Desktop. El comando de distribución usa `PathMap` global para sanear también las referencias al PDB dentro de las DLL. Los símbolos siguen generándose para desarrollo y pruebas. La salida actual es `.tools/desktop/publish-clean/`; las salidas anteriores `publish/` y `publish-no-symbols/` no son distribuibles.

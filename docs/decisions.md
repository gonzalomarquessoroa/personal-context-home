# Registro de decisiones

Decisiones propuestas en Fase 0, 29 de septiembre de 2026. Las sujetas a prueba se revisarán antes de construir sobre ellas.

## D-001 · .NET 10 para el núcleo y WPF para la interfaz futura

La primera distribución será solo Windows. WPF permite una interfaz nativa sencilla con una sola plataforma y sin runtime web o Node instalado por el usuario. .NET 10 tiene soporte hasta noviembre de 2028 según [Microsoft Lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core). El núcleo ya es independiente de WPF para facilitar pruebas. El SDK .NET 10 se instaló localmente para el desarrollo; no se implementa interfaz en Fase 1.

## D-002 · SQLite local sin ORM (implementada)

Una base por usuario evita servidor y credenciales. `Microsoft.Data.Sqlite` proporciona acceso directo y ligero. Usaremos SQL explícito y migraciones pequeñas; introducir un ORM solo si reduce complejidad real. La base reside en el perfil local, no se cifra en la primera fase. [Referencia de Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/).

## D-003 · Captura web con extensión MV3 + Native Messaging (condicional)

El historial de ChatGPT usado en la web no aparece documentado como recurso de la API de OpenAI. La [Conversations API](https://developers.openai.com/api/docs/guides/conversation-state) gestiona conversaciones de aplicaciones que usan la API. **Inferencia:** no es una vía soportada para sincronizar el historial personal de ChatGPT. Además, las [guías de plugins de OpenAI](https://developers.openai.com/plugins/app-guidelines) prohíben que un servidor MCP reconstruya todo el historial desde el cliente; un plugin no resuelve este caso.

Una extensión puede observar la página con consentimiento y enviar observaciones a un host local mediante [Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging). Se evita la dependencia de APIs privadas, cookies y un servidor local expuesto. Sigue existiendo dependencia del HTML de ChatGPT: se acepta solo si un prototipo demuestra cobertura suficiente, errores visibles y mantenimiento razonable. No se prometerá captura de app de escritorio o móvil.

**Puerta de decisión:** probar conversaciones nuevas, continuación, renombrado, edición, regeneración, ramas, proyectos y reinicio del navegador. Si fallan identidad o detección de forma sistemática, detener la integración y replantear el alcance; la importación manual no se convertirá por defecto en sincronización periódica.

## D-004 · Publicación autocontenida e instalador por usuario (propuesta)

Publicar `win-x64 --self-contained` evita instalar .NET al usuario. Un instalador Inno Setup es candidato porque debe copiar archivos y registrar el host Native Messaging en HKCU sin pedir administración. [Microsoft](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path) recoge esta opción y advierte que las actualizaciones requieren trabajo propio. Primero se verificará la instalación en una máquina limpia. La extensión probablemente requerirá una instalación consciente desde la tienda del navegador; el instalador deberá guiar y verificar la conexión. Las advertencias SmartScreen y la firma de código afectan la prueba con usuarios no técnicos.

## D-005 · Sin LLM en el núcleo inicial (decidido)

Ingestión, deduplicación exacta, persistencia, búsqueda textual y exportación no necesitan un modelo. Extracción semántica, equivalencias y contradicciones se evaluarán más adelante con lotes pequeños de mensajes nuevos y coste medido. Nunca enviar conversaciones completas a un servicio externo por defecto.

## D-006 · Repositorio público, datos locales fuera de Git (decidido)

El código se publicará en GitHub sin conversaciones, bases de datos, importaciones, exportaciones, SDK local ni secretos. `.gitignore` excluye estas rutas habituales, pero no sustituye la revisión de archivos y del historial antes del primer push. El autor de los commits locales usa la dirección privada `noreply` de GitHub. El primer commit se reescribió antes de crear un remoto para retirar el correo personal de la historia que se publicará. Véase [GitHub Docs: commit email](https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address).

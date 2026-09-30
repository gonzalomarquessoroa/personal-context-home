# Arquitectura

## Estado real tras implementar Fase 2.1

Existen `PersonalContext.Core`, `PersonalContext.Storage` y pruebas xUnit. El núcleo valida observaciones normalizadas y guarda proveedores, proyectos, conversaciones, mensajes, revisiones y estado de captura en SQLite. Usa `PRAGMA user_version = 1` para la primera migración, transacciones e índices de identidad. Las pruebas cubren reapertura, inicialización concurrente, reintentos idempotentes, 120 mensajes seguidos de 10 nuevos, revisiones, proyecto, lote inválido y estado de error. Existe una extensión MV3 y un host Native Messaging **experimentales** que no escriben en SQLite. Existe una interfaz WPF mínima de diagnóstico; no existen importador ni instalador.

El prototipo usa `activeTab`: un clic inyecta el observador únicamente en la pestaña actual de `chatgpt.com`; otro clic lo detiene. El observador envía al host IDs, rol conocido o `unknown`, orden y proyecto, sin texto. El host compara IDs visibles por pestaña y conversación, y devuelve cantidades de IDs añadidos o ya no visibles. No hay archivo de captura ni llamada de red. El enlace real Chrome–host se verificó con chats ficticios, incluidos reinicio del navegador y proyectos; la reducción posterior del protocolo pasó pruebas locales y sigue pendiente de otra prueba real de Chrome. El DOM actual no ofreció roles fiables para los mensajes probados. La vista puede omitir ramas, mensajes fuera de pantalla y contenido de adjuntos; se cuenta localmente el texto visible que excede 8192 caracteres, pero no se envía. Por ello el prototipo no usa el contrato `ProviderObservation` ni actualiza la base.

La [prueba real de Chrome](chatgpt-capture-spike.md) detectó IDs de mensajes visibles y de proyecto, pero también versiones nuevas con IDs distintos tras editar o regenerar, roles `unknown` y nodos ocultos conservados durante la navegación. El filtro de nodos ocultos funcionó al alternar dos chats ficticios; los contadores acumulados aún pueden incluir estados transitorios. El esquema actual **no representa la rama activa ni la cobertura de versiones ocultas**. Sigue siendo un núcleo probado con datos normalizados; todavía no es un capturador seguro de conversaciones reales.

## Desktop implementado · Fase 2.1

`PersonalContext.Desktop` está añadido a la solución: WinExe, `net10.0-windows`, WPF y referencia a Storage. `MainWindow.Loaded` inicia una única consulta con `Task.Run`; `GetConversations()` inicializa LocalStore y devuelve las conversaciones cuyo número se muestra. El trabajo SQLite ocurre fuera del hilo de interfaz y la continuación actualiza los controles en ese hilo. No hay SQL ni reglas de ingestión en Desktop, ni cambios en Core, Storage o la migración 001.

La ventana muestra estado de apertura, ruta seleccionable y contador. Si falla, conserva la ruta, marca el contador como no disponible y explica permisos, bloqueo, versión incompatible, archivo dañado o carga del paquete. No elimina ni reemplaza la base y no reintenta automáticamente. La captura de ChatGPT todavía no está conectada; el aviso aparece siempre.

El paquete es una carpeta `win-x64` autocontenida, sin single-file ni trimming, en `.tools/desktop/publish-clean/`. El publish excluye los PDB y el comando aplica `PathMap` global a Desktop y sus referencias para sanear las rutas de depuración de las DLL; los símbolos de build se conservan. La anterior `.tools/desktop/publish/` no es distribuible. La base continúa en el perfil local de Windows. El usuario confirmó el smoke manual en una cuenta local estándar de prueba de Windows 11 Home 25H2, desde una ruta con espacios, con reapertura y contadores 0/2, error visible ante una fixture SQLite inválida y recuperación del contador 2 tras restaurar la fixture válida. Es una observación del usuario, separada de las comprobaciones automáticas; el [registro detallado](tasks.md#smoke-manual-de-fase-21--confirmación-del-usuario) recoge su alcance. Sigue pendiente la prueba en una máquina sin .NET instalado; Fase 2 no está completada.

## Componentes previstos

El diagrama siguiente representa los componentes previstos, no los ya implementados.

```text
ChatGPT en Chrome o Edge
    └─ extensión MV3 (solo origen ChatGPT)
         └─ Native Messaging
              └─ host local → normalización → SQLite
                                      │
                                      └─ aplicación WPF → revisión / búsqueda / exportación
```

El núcleo C# será una biblioteca compartida por el host y la interfaz. El host no necesita que la ventana esté abierta: el navegador lo inicia al enviar datos. La comunicación usa mensajes JSON con longitud prefijada; el proceso local confirma la escritura para que la extensión pueda reintentar. El instalador registrará el host por usuario en Windows. Chrome exige `allowed_origins` con ID de extensión concreto; Edge tiene registro equivalente. Fuentes: [Chrome Native Messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging), [Edge Native Messaging](https://learn.microsoft.com/en-us/microsoft-edge/extensions/developer-guide/native-messaging).

### Límites de cada pieza

- **Extensión:** observa solo páginas autorizadas de ChatGPT y lee identificadores representados en la página. Detecta cambios de navegación y del DOM. No lee cookies, `localStorage`, peticiones internas ni endpoints no documentados. El script de contenido pasa datos al service worker; este limita los campos y comprueba origen y tamaño antes de enviarlos al host. El host valida el formato, tal como recomienda [Chrome](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging).
- **Host local:** valida tamaño y esquema, registra observaciones normalizadas y devuelve confirmación o error. No ofrece un servidor HTTP ni recibe tráfico de red.
- **Núcleo:** reglas de identidad, upsert, revisiones, migraciones, consulta y exportación. No depende de WPF ni de la estructura HTML del proveedor.
- **Interfaz:** muestra estado y permite control humano. No contiene reglas de ingestión.

El permiso de la extensión se limitará a los dominios concretos que requiera ChatGPT. El listado exacto debe fijarse tras el prototipo. Los [content scripts de Chrome](https://developer.chrome.com/docs/extensions/develop/concepts/content-scripts) son el mecanismo documentado para actuar dentro de páginas web; su acceso a la presentación de ChatGPT no constituye un contrato estable de datos.

## Ingestión incremental

Un adaptador entrega `ProviderObservation` con proveedor, clave de conversación, título, clave opcional de proyecto y mensajes observados. Cada mensaje incluye rol, orden, contenido, clave de origen si existe y huella del contenido. El núcleo aplica una transacción por lote:

1. Identifica la conversación mediante `(provider, external_conversation_id)`.
2. Inserta solo mensajes nuevos; una observación idéntica no cambia la base.
3. Si un mensaje conocido cambia, crea una revisión y deja rastro de la anterior.
4. Registra el resultado y solo entonces confirma al emisor.
5. La extracción de memoria futura recibe exclusivamente inserciones o revisiones nuevas.

**Leer no equivale a reprocesar:** al reabrir una conversación puede ser necesario recorrer los mensajes visibles para compararlos, porque no hay un cursor oficial de cambios. El objetivo incremental firme es no duplicar almacenamiento ni volver a ejecutar extracción sobre contenido igual.

Los IDs observados persistieron tras recargar un chat ficticio, pero las ediciones y regeneraciones generaron otros. El adaptador no inventará identificadores duraderos a partir del texto sin validar colisiones, ediciones, regeneraciones y ramas. Necesita estado explícito de cobertura incompleta y una política para rol desconocido antes de usar `ProviderObservation` con datos personales.

## Modelo de datos inicial propuesto

Se implementará de forma gradual. Campos `id` son claves locales; `external_*` solo se rellena cuando el proveedor ofrece una identidad fiable.

| Entidad | Campos iniciales | Restricción principal |
| --- | --- | --- |
| `providers` | `id`, `key`, `display_name` | `key` única (`chatgpt`) |
| `projects` | `id`, `provider_id`, `external_id`, `name`, `observed_at` | `(provider_id, external_id)` único cuando se conozca |
| `conversations` | `id`, `provider_id`, `external_id`, `project_id?`, `title`, `first_seen_at`, `last_seen_at` | `(provider_id, external_id)` único |
| `messages` | `id`, `conversation_id`, `source_key`, `role`, `ordinal`, `body`, `body_hash`, `observed_at` | `(conversation_id, source_key)` único |
| `message_revisions` | `id`, `message_id`, `role`, `ordinal`, `body`, `body_hash`, `observed_at`, `replaced_at` | historial de cambios del mensaje |
| `memories` | `id`, `category`, `title`, `content`, `status`, `confidence?`, `valid_from?`, `valid_to?`, `created_at`, `updated_at` | estado `pending / confirmed / outdated / conflicting` |
| `memory_sources` | `memory_id`, `message_id`, `evidence_note?` | procedencia de varios mensajes |
| `capture_state` | `provider_id`, `conversation_id?`, `last_observed_at`, `last_success_at`, `last_error?`, `coverage` | estado visible para diagnóstico |

`source_key` es obligatorio en el contrato interno y estable entre reintentos; el adaptador debe declarar cómo lo obtuvo. Si el prototipo no logra una clave segura, el esquema deberá evolucionar antes de capturar datos de uso real. `Person`, `Goal`, `Decision`, `Preference` y `Tool` serán **categorías o relaciones de memoria**, no tablas independientes en V0. Un proyecto solo se guarda cuando el adaptador conoce su `external_id`; la conversación puede conservarse sin proyecto. `Memory` y sus fuentes se crearán en la fase de memoria; aún no forman parte de la migración 001.

## Datos y seguridad

- Base en `%LOCALAPPDATA%\PersonalContextHome\data\`, nunca junto al ejecutable. Copias de seguridad y exportaciones se elegirán explícitamente.
- SQLite mediante `Microsoft.Data.Sqlite`, con migraciones versionadas y transacciones. Fuente: [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/).
- Por defecto la base **no estará cifrada**: tendrá las protecciones de la cuenta Windows y del disco. No se afirmará cifrado inexistente. Antes de usuarios externos se evaluará cifrado en reposo y copias de seguridad.
- Sin secretos de ChatGPT ni tokens de sesión. Si en el futuro hay claves propias, se protegerán con mecanismos de Windows; nunca en texto plano.
- Exportaciones JSON/Markdown contienen datos personales legibles. La interfaz debe avisarlo y permitir borrar todos los datos locales.
- Ningún LLM externo procesa conversaciones en Fases 0–6. En fases posteriores, cualquier envío fuera del dispositivo exigirá explicación y consentimiento explícito.

## Estructura prevista del repositorio

```text
README.md
docs/
  project.md
  architecture.md
  decisions.md
  roadmap.md
  tasks.md
src/
  PersonalContext.Core/          # entidades, reglas e ingestión
  PersonalContext.Storage/       # SQLite y migraciones
  PersonalContext.Desktop/       # WPF mínimo de diagnóstico de LocalStore
  PersonalContext.NativeHost/    # puente de la extensión
  providers/chatgpt/extension/   # MV3; después de la prueba de viabilidad
installer/                       # receta de instalador
tests/                           # pruebas de comportamiento del núcleo
```

Crear cada carpeta solo al implementar su fase. Evitamos proyectos vacíos y abstracciones de proveedores que todavía no existen.

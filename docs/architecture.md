# Arquitectura

## Estado real al terminar la Fase 0

Solo existen documentación y decisiones. No hay ejecutables, esquema SQLite, extensión, instalador ni pruebas de captura. El diseño siguiente es el objetivo mínimo para las próximas fases.

## Componentes previstos

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

- **Extensión:** observa solo páginas autorizadas de ChatGPT y extrae contenido ya representado en la página. Detecta cambios de navegación y del DOM. No lee cookies, `localStorage`, peticiones internas ni endpoints no documentados. El script de contenido pasa datos al service worker; este valida origen y formato antes de enviarlos al host, tal como recomienda [Chrome](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging).
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

Las claves de mensaje de una página web pueden no ser estables. El adaptador no inventará identificadores duraderos a partir del texto sin validar colisiones, ediciones, regeneraciones y ramas. La prueba de captura debe decidir si existe una clave fiable o si hace falta una identidad provisional basada en posición y revisión, con estado explícito de cobertura incompleta.

## Modelo de datos inicial propuesto

Se implementará de forma gradual. Campos `id` son claves locales; `external_*` solo se rellena cuando el proveedor ofrece una identidad fiable.

| Entidad | Campos iniciales | Restricción principal |
| --- | --- | --- |
| `providers` | `id`, `key`, `display_name` | `key` única (`chatgpt`) |
| `projects` | `id`, `provider_id`, `external_id`, `name`, `observed_at` | `(provider_id, external_id)` único cuando se conozca |
| `conversations` | `id`, `provider_id`, `external_id`, `project_id?`, `title`, `first_seen_at`, `last_seen_at` | `(provider_id, external_id)` único |
| `messages` | `id`, `conversation_id`, `source_key`, `role`, `ordinal`, `body`, `body_hash`, `observed_at` | `(conversation_id, source_key)` único |
| `message_revisions` | `id`, `message_id`, `body`, `body_hash`, `observed_at` | historial de cambios de contenido |
| `memories` | `id`, `category`, `title`, `content`, `status`, `confidence?`, `valid_from?`, `valid_to?`, `created_at`, `updated_at` | estado `pending / confirmed / outdated / conflicting` |
| `memory_sources` | `memory_id`, `message_id`, `evidence_note?` | procedencia de varios mensajes |
| `capture_state` | `provider_id`, `conversation_id?`, `last_observed_at`, `last_success_at`, `last_error?`, `coverage` | estado visible para diagnóstico |

`source_key` será obligatorio en el contrato interno y estable entre reintentos; el adaptador debe declarar cómo la obtuvo. Si el prototipo no logra una clave segura, el esquema podrá incorporar una identidad provisional y revisión antes de capturar datos de uso real. `Person`, `Goal`, `Decision`, `Preference` y `Tool` son **categorías o relaciones de memoria**, no tablas independientes en V0. `Project` puede quedar sin `external_id` hasta confirmar una señal fiable. `Memory` y sus fuentes se crearán en la fase de memoria, no en la primera migración si aún no se usan.

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
  PersonalContext.Desktop/       # WPF; se añade cuando haya flujo visible
  PersonalContext.NativeHost/    # puente de la extensión
  providers/chatgpt/extension/   # MV3; después de la prueba de viabilidad
installer/                       # receta de instalador
tests/                           # pruebas de comportamiento del núcleo
```

Crear cada carpeta solo al implementar su fase. Evitamos proyectos vacíos y abstracciones de proveedores que todavía no existen.

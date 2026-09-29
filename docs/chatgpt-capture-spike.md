# Prueba exploratoria de captura de ChatGPT

Fecha: 29 de septiembre de 2026. Entorno: ChatGPT web en Chrome con sesión iniciada. Todas las observaciones se hicieron con conversaciones, proyecto y adjunto **ficticios**. La exploración inicial usó un proyecto y dos conversaciones que se eliminaron al terminar; la prueba posterior de la extensión usó nuevos datos ficticios. No se abrieron conversaciones personales para la prueba ni se guardó contenido de ChatGPT en el repositorio.

## Qué se observó

| Caso | Resultado observado | Consecuencia |
| --- | --- | --- |
| Chat nuevo | La ruta cambió a `/c/{id}`. Los dos mensajes visibles tenían identificadores de origen en `data-chatgpt-search-message-ids`. | La conversación y los mensajes visibles pueden identificarse en esta versión de la web. |
| Recarga | Después de cargar de nuevo la página, se conservaron la ruta y los identificadores de ambos mensajes. | Los identificadores funcionaron entre cargas de la misma conversación de prueba. No demuestra estabilidad a largo plazo. |
| Continuación | Aparecieron dos identificadores nuevos; los anteriores quedaron iguales. | La deduplicación por ID puede distinguir esta continuación. |
| Edición de mensaje de usuario | La versión editada recibió **otro** identificador. La versión previa dejó de estar en la rama principal visible y apareció el control «See versions». | Una edición no debe modelarse automáticamente como sustitución del mismo ID. El historial de ramas y la rama activa requieren representación explícita. |
| Regeneración de respuesta | La respuesta nueva recibió **otro** identificador; la anterior dejó de estar en la rama principal. | Comparar solo la página actual no permite deducir si un mensaje desaparecido fue borrado, sustituido o quedó en otra rama. |
| Cambio de título | El título cambió en la barra lateral y el ID de conversación de la ruta se conservó. | El título puede actualizarse por ID de conversación. |
| Chat dentro de proyecto nuevo | La ruta contenía `/g/{id-y-slug}/c/{id}`; el breadcrumb enlazaba a `/g/{id}/project`. | La pertenencia al proyecto es observable en esta vista sin inferirla a partir del título. |

Los nodos de respuesta observados repetían el mismo UUID dos veces, separado por un espacio, en `data-chatgpt-search-message-ids`. Por tanto, ese atributo necesita normalización y validación; no se puede usar literalmente como una clave única. `data-chatgpt-search-unit-key` contenía claves posicionales `fallback-turn-...`; su nombre y su valor indican una clave de presentación, no una identidad duradera. Los selectores y atributos son detalles internos de la página, **sin contrato de estabilidad**.

## Prototipo de extensión y puente local

La extensión de `providers/chatgpt/extension/` usa Manifest V3, `activeTab`, `scripting` y `nativeMessaging`. Se inyecta al pulsar su botón en una pestaña de `https://chatgpt.com`; otro clic la detiene. Un `MutationObserver` y una comprobación de ruta observan el chat activo. El script toma IDs de `data-chatgpt-search-message-ids`, intenta determinar el rol y envía texto de los mensajes visibles al host. Limita el texto a 8192 caracteres por mensaje y marca la cobertura como `visible_dom_only`. Detecta un proyecto de la ruta `/g/g-p-<32 hexadecimales>-.../c/...` o del enlace al proyecto. Filtra nodos ocultos que ChatGPT puede conservar al cambiar de chat dentro de la misma pestaña. Todos estos selectores son detalles internos sin garantía de estabilidad.

`PersonalContext.NativeHost` recibe tramas JSON de longitud prefijada, valida tamaño, IDs de conversación y mensaje, rol y orden, y comprueba el formato del proyecto para indicarlo en la respuesta. Compara en memoria conjuntos de IDs por conversación. Devuelve solo cantidades de mensajes visibles, nuevos y ya no visibles, y si observó proyecto. **Descarta el texto, mantiene los IDs solo en memoria y no escribe archivos ni SQLite.** Acepta `unknown` cuando el DOM no permite determinar el rol. `noLongerVisible` significa ausencia en la observación actual, nunca borrado. El host pierde su estado al terminar su proceso.

El usuario cargó manualmente la extensión desempaquetada en Chrome y el script `scripts/register-probe-host.ps1` registró el host en HKCU para ese ID local. Se comprobó el intercambio real extensión–host: el recuadro de la página recibió respuestas del host al observar chats ficticios. La automatización del navegador quedó bloqueada por la revisión automática de acceso a `chrome://extensions/` y a ChatGPT; el usuario hizo los pasos de Chrome y comunicó los contadores. Esa revisión no se eludió.

## Resultado y límites de la prueba real

| Caso | Evidencia comunicada desde el recuadro PCH | Límite |
| --- | --- | --- |
| Chat ficticio y continuación | Tras adaptar el observador al DOM actual, contó 2 mensajes y después 6 al añadir continuaciones. Detectó los IDs nuevos en observaciones sucesivas. | El DOM no expuso roles fiables: los 6 quedaron como `unknown`. |
| Edición | Con 6 visibles, el acumulado pasó de `+6/-0` a `+8/-2` tras editar una pregunta y esperar la respuesta. | Los IDs retirados pueden seguir en otra versión; el contador no indica borrado. |
| Regeneración | Con 6 visibles, el acumulado pasó a `+9/-3` tras regenerar una respuesta. | No se comprobó la recuperación de la respuesta anterior. |
| Recarga de página | En la misma sesión del host, 6 visibles dieron `+0/-0` tras recargar. | Solo demuestra estabilidad de esos IDs durante esta prueba. |
| Reinicio de Chrome | Tras cerrar Chrome y abrir de nuevo el chat ficticio con adjunto, el host contó 4 visibles, `+4/-0`, `proyecto sí` y 0 nodos ocultos. | El primer `+4` es un nuevo punto de partida en memoria; no demuestra que se capturase actividad mientras Chrome estuvo cerrado. |
| Proyecto | Tras mover el chat ficticio a un proyecto, la ruta incluyó `g-p-` más 32 caracteres hexadecimales. Con el analizador corregido, el host confirmó `proyecto sí`. Un chat nuevo del proyecto también se asoció. | No se conoce el comportamiento en otros formatos o cuentas. |
| Navegación entre chats del proyecto | Chat nuevo: 2 visibles; chat anterior: 6 visibles y 2 nodos ocultos; vuelta al nuevo: 2 visibles y 6 nodos ocultos, sin recargar. | Hubo un falso recuento de 8 antes de filtrar el DOM oculto. Las transiciones aún pueden alterar los acumulados. |
| Adjunto ficticio | Tras enviar un archivo de texto ficticio y recibir respuesta, el chat nuevo mostró 4 mensajes visibles y `proyecto sí`. | Solo se probaron IDs de los mensajes; el prototipo no extrae ni verifica contenido o metadatos del adjunto. El acumulado `+12/-2` mostró cambios transitorios, por lo que no es una medida fiable de actividad. |
| Versiones antiguas | La interfaz ofrecía «Branch in a new chat» o «Return to current version»; no se cambió de versión en el mismo chat. | Navegación de ramas y restauración sin crear otro chat, sin verificar. |

El recuadro llegó a mostrar `0 sin ID`, `6 rol desconocido` y `0 truncados` en el chat de seis mensajes. Por tanto, **identificar mensajes visibles funciona en estos casos, pero identificar quién habló no**. No se debe asignar automáticamente el rol de los mensajes desconocidos ni guardar estos lotes como conversaciones personales. Los contadores acumulados suman diferencias entre observaciones y pueden incluir estados intermedios de la interfaz; no representan un historial fiable.

No se verificaron chats largos con mensajes fuera de pantalla, todas las ramas históricas, recuperación tras fallo de entrega, otros formatos de adjuntos, otras cuentas ni actividad mientras Chrome está cerrado. El reinicio completo sí se probó, con una nueva observación inicial. La extensión requiere activación manual en cada pestaña; no satisface aún el objetivo de captura automática. La instalación mediante extensión desempaquetada, registro manual del host y comandos de desarrollo tampoco satisface el objetivo de configuración para usuarios finales. ChatGPT de escritorio y móvil quedan fuera de esta prueba.

El núcleo SQLite puede guardar mensajes por `source_key`, pero aún no representa *rama activa*, *versión alternativa* o *cobertura parcial*. Ingerir solo la rama visible podría dejar mensajes antiguos aparentemente vigentes y producir recuerdos contradictorios. **La puerta para conectar el adaptador a datos personales sigue cerrada** hasta resolver roles, ramas y cobertura verificable. MV3 + Native Messaging queda validado como transporte local de prueba, no como captura completa.

## Comprobaciones locales

| Verificación del 29-09-2026 | Resultado |
| --- | --- |
| Suite .NET | 11 pruebas correctas. |
| Host autocontenido mediante protocolo Native Messaging | Ping, entrega idéntica, cambio de ID, proyecto ficticio y rol `unknown` correctos. |
| Observador contra DOM ficticio | Continuación, edición, regeneración, proyecto por ruta y breadcrumb, ID ambiguo, texto truncado y nodos ocultos correctos. |

El ejecutable autocontenido de desarrollo y el archivo de adjunto ficticio se guardaron bajo `.tools/`, excluido de Git. No se incluye en el repositorio el ID de extensión local, URLs de chats de prueba ni contenido de conversaciones.

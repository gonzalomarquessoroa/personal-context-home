# Prueba exploratoria de captura de ChatGPT

Fecha: 29 de septiembre de 2026. Entorno: ChatGPT web en Chrome con sesión iniciada. Se crearon únicamente un proyecto y dos conversaciones **ficticios**, que se eliminaron al terminar. No se abrieron conversaciones personales ni se guardó contenido real en el repositorio.

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

## Cobertura y límites

Esta fue una prueba de lectura del DOM de la página mediante el navegador de desarrollo. **Todavía no se ha instalado ni verificado la extensión de Personal Context Home en Chrome.** El host Native Messaging sí se ha verificado como proceso independiente con lotes ficticios; falta comprobar el enlace con Chrome. Tampoco se probaron reinicio completo de Chrome, chats largos con mensajes fuera de pantalla, adjuntos, cambios de proyecto de un chat existente, versiones antiguas navegadas desde la interfaz, otras cuentas, ChatGPT de escritorio o móvil. La prueba no demuestra que el sistema pueda capturar actividad ocurrida mientras Chrome está cerrado.

El núcleo SQLite actual puede guardar mensajes nuevos por `source_key`, pero carece de conceptos de *rama activa*, *versión alternativa* y *cobertura parcial*. Con la edición y regeneración observadas, ingerir directamente solo la rama visible dejaría mensajes antiguos aparentemente vigentes y podría producir recuerdos contradictorios. **No conectar el adaptador real a datos personales hasta resolver ese modelo y medir los casos restantes.**

## Siguiente experimento mínimo

1. Completar en Chrome la prueba de la extensión manual limitada a `chatgpt.com`, que observa el DOM renderizado sin cookies, almacenamiento de sesión ni API interna.
2. Comprobar el enlace real extensión–host, la confirmación, el reintento y el reinicio del navegador. El protocolo local con lotes sintéticos ya pasó una prueba independiente.
3. Repetir edición, regeneración y cambio de rama; registrar qué versiones son visibles y cómo representar actividad y cobertura sin inventar datos.
4. Probar un chat largo y uno con adjuntos, y definir un estado de salud comprensible cuando la captura sea parcial o falle.
5. Evaluar el flujo real de instalación de extensión y host para alguien sin conocimientos técnicos antes de comprometer la arquitectura final.

**Conclusión provisional:** hay señales útiles para chats activos en esta versión web, pero la captura completa y mantenible sigue sin estar demostrada. La hipótesis MV3 + Native Messaging continúa como experimento, no como promesa de producto.

## Prototipo de extensión y puente local

La extensión de `providers/chatgpt/extension/` usa Manifest V3, `activeTab`, `scripting` y `nativeMessaging`. Solo se inyecta tras pulsar su botón en una pestaña de `https://chatgpt.com`; un segundo clic la detiene. Un `MutationObserver` y una comprobación de ruta observan el chat activo. El script intenta extraer un UUID único de `data-chatgpt-search-message-ids`, rol y texto de cada mensaje visible, además del ID de proyecto cuando figura en la ruta o breadcrumb. Descarta atributos ambiguos, informa cuántos nodos omitió y trunca texto largo. Esos selectores son detalles internos de ChatGPT y aún **no se han validado desde la extensión**.

`PersonalContext.NativeHost` lee tramas JSON de longitud prefijada en stdin y escribe una respuesta en stdout. Valida UUID, rol, orden, duplicados y tamaño. En memoria compara los IDs visibles por conversación y confirma `visible`, `added` y `noLongerVisible`. No devuelve texto ni IDs, no escribe archivos y no toca SQLite. `noLongerVisible` significa solo ausencia en la observación actual: **no implica borrado**. Reiniciar el proceso borra la comparación previa; los contadores de la primera observación vuelven a empezar.

| Verificación del 29-09-2026 | Resultado |
| --- | --- |
| Compilación del host y suite .NET | Correcta: 11 pruebas, 0 fallos. |
| Sintaxis de ambos scripts MV3 | Correcta con `node --check`. |
| Host autocontenido por protocolo binario real | Correcto: ping, entrega idéntica, cambio de ID por edición, cambio de ID por regeneración y proyecto ficticio. |
| Observador contra DOM ficticio | Correcto: proyecto por ruta y breadcrumb, continuación, edición, regeneración, ID ambiguo omitido y texto largo truncado. No demuestra compatibilidad con el DOM actual de ChatGPT. |
| Extensión cargada y lote real desde ChatGPT | **Pendiente.** El control de Chrome bloqueó `chrome://extensions/`; no se intentó otra vía de instalación. |
| Nueva prueba web en la cuenta conectada | **Pendiente.** La revisión automática impidió abrir la portada de ChatGPT porque podría mostrar conversaciones personales. El usuario compartió una URL concreta de chat ficticio, pero la revisión también bloqueó el acceso por tratarlo como acceso al origen general. No se abrió el chat. |

El script `scripts/register-probe-host.ps1` crea el manifiesto local y la clave HKCU para un ID de extensión concreto, una vez disponible. No se ejecutó ni se registró un host en esta prueba. El ejecutable autocontenido de desarrollo queda bajo `.tools/`, excluido de Git. La prueba no valida persistencia, recuperación tras cierre, identidad de ramas ocultas ni extracción completa de contenido.

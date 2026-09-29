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

Esta fue una prueba de lectura del DOM de la página mediante el navegador de desarrollo. **Todavía no se ha instalado ni verificado una extensión de Personal Context Home ni un host Native Messaging.** Tampoco se probaron reinicio completo de Chrome, chats largos con mensajes fuera de pantalla, adjuntos, cambios de proyecto de un chat existente, versiones antiguas navegadas desde la interfaz, otras cuentas, ChatGPT de escritorio o móvil. La prueba no demuestra que el sistema pueda capturar actividad ocurrida mientras Chrome está cerrado.

El núcleo SQLite actual puede guardar mensajes nuevos por `source_key`, pero carece de conceptos de *rama activa*, *versión alternativa* y *cobertura parcial*. Con la edición y regeneración observadas, ingerir directamente solo la rama visible dejaría mensajes antiguos aparentemente vigentes y podría producir recuerdos contradictorios. **No conectar el adaptador real a datos personales hasta resolver ese modelo y medir los casos restantes.**

## Siguiente experimento mínimo

1. Crear una extensión de prueba limitada a `chatgpt.com` que observe el DOM renderizado, sin cookies, almacenamiento de sesión ni API interna.
2. Enviar un lote sintético al host local por Native Messaging y comprobar confirmación, reintento, reinicio del navegador y límites de tamaño.
3. Repetir edición, regeneración y cambio de rama; registrar qué versiones son visibles y cómo representar actividad y cobertura sin inventar datos.
4. Probar un chat largo y uno con adjuntos, y definir un estado de salud comprensible cuando la captura sea parcial o falle.
5. Evaluar el flujo real de instalación de extensión y host para alguien sin conocimientos técnicos antes de comprometer la arquitectura final.

**Conclusión provisional:** hay señales útiles para chats activos en esta versión web, pero la captura completa y mantenible sigue sin estar demostrada. La hipótesis MV3 + Native Messaging continúa como experimento, no como promesa de producto.

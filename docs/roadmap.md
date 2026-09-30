# Roadmap

El orden es orientativo. Cada fase termina con una capacidad verificable, no solo con archivos creados.

| Fase | Resultado | Estado |
| --- | --- | --- |
| 0 | Alcance, stack, modelo, estrategia de captura y backlog | Completada en documentación; captura por validar |
| 1 | Núcleo local, esquema mínimo, ingestión idempotente y prueba limitada de captura | Completada: núcleo probado y transporte Chrome–host verificado; límites documentados |
| 2 | Primer ejecutable e instalación sin prerequisitos para el usuario | Pendiente |
| 3 | Resolver cobertura de ChatGPT y, si es viable, construir el adaptador de navegador | Pendiente; roles, ramas, chats largos y recuperación son puerta de viabilidad |
| 4 | Flujo incremental y recuperación ante fallos de entrega | Pendiente |
| 5 | Conversaciones y proyectos, según señales comprobadas | Pendiente |
| 6 | Importación histórica opcional, una sola vez | Pendiente |
| 7–8 | Memoria estructurada, revisiones, duplicados y cambios | Pendiente |
| 9–10 | Interfaz simple y bandeja de revisión | Pendiente |
| 11–12 | Búsqueda trazable y contexto copiable/exportable | Pendiente |
| 13–14 | Otros proveedores, solo tras estabilizar ChatGPT | Pendiente |
| 15–17 | Onboarding <15 min; pruebas con personas no técnicas y siete días sin abrir la app | Pendiente |
| 18 | Home V1 | Pendiente |

## Puertas para declarar V1

La captura automática debe medirse en un navegador soportado y mostrarse con estado de salud comprensible. Se debe registrar cobertura y lagunas de contenido, especialmente proyectos, cambios anteriores, otros dispositivos y app de escritorio. La prueba de instalación debe incluir el paso real de extensión y las advertencias de Windows. Una V1 no puede declararse completa solo porque el importador histórico funcione.

## Incremento Fase 2.1

Implementada la ventana WPF mínima .NET 10 en la solución: abre LocalStore en segundo plano, muestra estado, ruta y número de conversaciones, y presenta errores comprensibles. Indica que la captura de ChatGPT todavía no está conectada. Publicada localmente la carpeta Windows x64 autocontenida, sin single-file ni trimming; el usuario confirmó el smoke manual en una cuenta local estándar de Windows 11 Home 25H2. Esta observación es distinta de las comprobaciones automáticas; véase el [registro](tasks.md#smoke-manual-de-fase-21--confirmación-del-usuario).

Fase 2 permanece **pendiente**. El smoke confirmado acredita la observación manual de arranque y respuesta normal, ruta con espacios, base bajo el perfil de prueba, reapertura con 0 y 2 conversaciones ficticias y error comprensible ante una fixture SQLite inválida, seguido de restauración válida con contador 2. Falta la ejecución en una máquina sin .NET instalado; no se acredita todavía la experiencia de instalación. Instalador y registro automático del puente son incrementos posteriores; el prototipo sigue aislado de SQLite.

# Producto y alcance

## Problema

Las conversaciones útiles quedan dispersas entre proveedores. Personal Context Home pretende conservar datos relevantes, estructurados y con procedencia, que una persona pueda revisar y reutilizar en otra IA.

## Usuario inicial

Una persona con Windows y conocimientos técnicos mínimos. La primera prueba será el propio autor; después, un amigo y sus padres. La instalación final no debe exigir terminal, Node, Git, variables de entorno ni una base de datos configurada a mano.

## Resultado buscado para Home V1

- Instalación y conexión en menos de 15 minutos.
- Captura incremental automática de chats de ChatGPT **en el navegador compatible y perfil donde esté instalada la extensión**.
- Conversaciones, mensajes y proyectos asociados cuando sea posible determinar esa relación.
- Memoria estructurada con fuentes, edición, eliminación y revisión de cambios dudosos.
- Búsqueda trazable, contexto copiable y exportación completa en JSON y Markdown.
- Todo el contenido almacenado localmente; ninguna conversación completa sale del equipo por defecto.
- Estado de salud visible: última captura, fallos y cobertura de la integración.

La cobertura de la app de escritorio de ChatGPT, otros navegadores y el móvil **no está demostrada**. No se presentará como incluida hasta tener una vía fiable. Tampoco puede prometerse la detección de actividad creada en otra sesión o dispositivo si nunca se abre en el navegador observado.

## Fuera de alcance de la primera versión

SaaS, cuentas, sincronización cloud, equipos, pagos, móvil, API pública, agentes autónomos y varios proveedores simultáneos. La importación de historial será opcional para el arranque, no el mecanismo de sincronización normal.

## Pruebas de producto

1. **Prueba de los padres:** instalador, extensión, conexión y explicación comprensible del estado en menos de 15 minutos, sin asistencia técnica presencial.
2. **Prueba de abandono:** tras siete días sin abrir Personal Context Home, los chats **usados en el navegador conectado** y sus mensajes nuevos aparecen al abrir la aplicación. Registrar explícitamente cualquier actividad fuera de esa cobertura.

Estas pruebas son criterios de aceptación futuros, no resultados ya alcanzados.

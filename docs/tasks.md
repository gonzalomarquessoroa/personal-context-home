# Backlog inmediato

## Fase 1 · Núcleo local

Orden sugerido para cambios pequeños y completos:

1. **Hecho: preparar solución y herramienta de desarrollo.** SDK .NET 10 local, solución, bibliotecas `Core` y `Storage`, xUnit, `.gitignore` e instrucciones de desarrollo. No afecta a la instalación final de usuario.
2. **Hecho: migración SQLite 001.** `providers`, `projects`, `conversations`, `messages`, `message_revisions` y `capture_state`, con claves únicas e índices. Base bajo `%LOCALAPPDATA%` por defecto y versión en `PRAGMA user_version`.
3. **Hecho: contrato de observación.** DTO normalizado independiente de ChatGPT y validación de tamaño, rol, claves e identificador de conversación. Los datos sintéticos de las pruebas sirven como origen local; aún sin extensión.
4. **Hecho: ingestión transaccional.** El mismo lote no duplica contenido; añadir 10 mensajes después de 120 añade solo 10. Los cambios de contenido, rol u orden guardan una revisión. Un lote inválido no escribe mensajes parciales.
5. **Hecho: consultas mínimas de diagnóstico.** Lectura de conversaciones, mensajes y última captura/error desde el núcleo. La utilidad de desarrollo queda opcional.
6. **Hecho: prueba real limitada de Chrome antes del adaptador completo.** La [prueba con chats ficticios](chatgpt-capture-spike.md) verificó el enlace extensión–host, chat nuevo, continuación, edición, regeneración, recarga, reinicio de Chrome, proyecto, navegación entre dos chats y un adjunto de texto ficticio. Encontró roles no identificables en el DOM actual, nodos ocultos durante navegación y contadores transitorios. Después se redujo el protocolo a IDs y metadatos, se eliminaron roles inferidos y se separó la comparación por pestaña; estos cambios pasaron pruebas locales ficticias, pero aún no una nueva prueba en Chrome. No se demostró captura completa ni automática. Chats largos, ramas históricas y recuperación de fallos pasan a la puerta de viabilidad de Fase 3.

## Criterios de aceptación de Fase 1

- El usuario final todavía no necesita instalar nada: esta fase es interna.
- La base se crea en la ruta local prevista y se migra sin perder datos.
- Dos entregas idénticas dejan una sola conversación y un solo mensaje por clave.
- Una entrega con 10 mensajes nuevos añade exactamente 10, y no dispara trabajo posterior para los 120 anteriores.
- Una edición conserva la versión previa y queda trazable.
- Las pruebas ejecutan sin red ni cuenta de OpenAI.
- `architecture.md`, `decisions.md` y este backlog se actualizan si la prueba cambia supuestos.

## Riesgos a cerrar pronto

1. **Cobertura real de ChatGPT:** la extensión encontró IDs en los mensajes visibles de los chats ficticios, pero no pudo determinar sus roles; el DOM también conservó nodos ocultos al navegar. Ediciones y regeneraciones crearon IDs nuevos. El modelo debe representar roles desconocidos, ramas y cobertura antes de captar datos reales.
2. **Proyectos:** se observó el formato de ruta `g-p-` seguido de 32 caracteres hexadecimales y el host lo confirmó en chats ficticios. Se guardará asociación solo cuando haya evidencia; otros formatos no están verificados.
3. **Instalación:** extensión y host deben enlazarse sin terminal; IDs de tienda y firma pueden complicar el flujo.
4. **Privacidad:** la base inicial no tiene cifrado de aplicación; explicar protección real, exportación legible y borrado.
5. **Entorno de desarrollo:** el SDK .NET 10 ya está instalado localmente en `.tools`; Inno Setup se necesitará al empaquetar, pero nunca en la máquina de los usuarios.
6. **Repositorio público:** no subir datos reales, archivos `.env`, bases locales ni exportaciones; revisar archivos y metadatos de autor antes de publicar nuevos commits.

## Próxima puerta de viabilidad

Antes de una captura personal o automática: identificar roles con evidencia, representar rama activa y versiones ocultas, medir chats largos y mensajes fuera de pantalla, mostrar cobertura y errores, y probar recuperación de entregas. El transporte local funciona; estas condiciones siguen pendientes.

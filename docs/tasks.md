# Backlog inmediato

## Fase 1 · Núcleo local

Orden sugerido para cambios pequeños y completos:

1. **Hecho: preparar solución y herramienta de desarrollo.** SDK .NET 10 local, solución, bibliotecas `Core` y `Storage`, xUnit, `.gitignore` e instrucciones de desarrollo. No afecta a la instalación final de usuario.
2. **Hecho: migración SQLite 001.** `providers`, `projects`, `conversations`, `messages`, `message_revisions` y `capture_state`, con claves únicas e índices. Base bajo `%LOCALAPPDATA%` por defecto y versión en `PRAGMA user_version`.
3. **Hecho: contrato de observación.** DTO normalizado independiente de ChatGPT y validación de tamaño, rol, claves e identificador de conversación. Los datos sintéticos de las pruebas sirven como origen local; aún sin extensión.
4. **Hecho: ingestión transaccional.** El mismo lote no duplica contenido; añadir 10 mensajes después de 120 añade solo 10. Los cambios de contenido, rol u orden guardan una revisión. Un lote inválido no escribe mensajes parciales.
5. **Hecho: consultas mínimas de diagnóstico.** Lectura de conversaciones, mensajes y última captura/error desde el núcleo. La utilidad de desarrollo queda opcional.
6. **En curso: prueba de viabilidad de ChatGPT antes del adaptador completo.** La [exploración del DOM con datos ficticios](chatgpt-capture-spike.md) comprobó chat nuevo, recarga, continuación, edición, regeneración, título y proyecto. Falta extensión local temporal + host que acepte un lote de ejemplo, además de reinicio de Chrome, chats largos, versiones y ruta viable de instalación para una persona no técnica. Esta prueba decide el diseño de Fase 3.

## Criterios de aceptación de Fase 1

- El usuario final todavía no necesita instalar nada: esta fase es interna.
- La base se crea en la ruta local prevista y se migra sin perder datos.
- Dos entregas idénticas dejan una sola conversación y un solo mensaje por clave.
- Una entrega con 10 mensajes nuevos añade exactamente 10, y no dispara trabajo posterior para los 120 anteriores.
- Una edición conserva la versión previa y queda trazable.
- Las pruebas ejecutan sin red ni cuenta de OpenAI.
- `architecture.md`, `decisions.md` y este backlog se actualizan si la prueba cambia supuestos.

## Riesgos a cerrar pronto

1. **Cobertura real de ChatGPT:** el DOM puede cambiar, ocultar mensajes o no exponer IDs estables. Esta es la condición principal de viabilidad.
   En la prueba inicial, las ediciones y regeneraciones crearon IDs nuevos y ocultaron las versiones anteriores en la rama principal. El modelo debe representar ramas y cobertura antes de captar datos reales.
2. **Proyectos:** la asociación puede no estar presente o ser ambigua en la página. Se guardará solo cuando haya evidencia.
3. **Instalación:** extensión y host deben enlazarse sin terminal; IDs de tienda y firma pueden complicar el flujo.
4. **Privacidad:** la base inicial no tiene cifrado de aplicación; explicar protección real, exportación legible y borrado.
5. **Entorno de desarrollo:** el SDK .NET 10 ya está instalado localmente en `.tools`; Inno Setup se necesitará al empaquetar, pero nunca en la máquina de los usuarios.
6. **Repositorio público:** no subir datos reales, archivos `.env`, bases locales ni exportaciones; comprobar también la dirección de autor en todo el historial antes del primer push.

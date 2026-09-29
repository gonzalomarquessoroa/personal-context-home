# Backlog inmediato

## Fase 1 · Núcleo local

Orden sugerido para cambios pequeños y completos:

1. **Preparar solución y herramienta de desarrollo.** Instalar SDK .NET 10 en el entorno de desarrollo; crear solución, biblioteca `Core`, biblioteca `Storage` y proyecto de pruebas. Añadir `.gitignore` de .NET y una instrucción de ejecución para desarrolladores. No afecta a la instalación final de usuario.
2. **Migración SQLite 001.** Crear `providers`, `projects`, `conversations`, `messages`, `message_revisions` y `capture_state` con claves únicas e índices. Crear la base bajo `%LOCALAPPDATA%` y registrar versión. Verificar creación en instalación limpia y reapertura sin cambios.
3. **Contrato de observación.** Definir DTO normalizado independiente de ChatGPT y validación de tamaño, rol, claves e identificador de conversación. Incluir un origen de prueba local; aún sin extensión.
4. **Ingestión transaccional.** Insertar un lote, reintentar el mismo sin duplicados, añadir mensajes nuevos y registrar una revisión cuando cambie el contenido de una clave conocida. Devolver conteos de insertados, actualizados y repetidos. Probar que un lote inválido no deja escritura parcial.
5. **Consultas mínimas de diagnóstico.** Leer conversaciones y mensajes guardados, y la última captura/error. Una pequeña utilidad de desarrollo puede ejercer el flujo mientras no exista WPF.
6. **Prueba de viabilidad de ChatGPT antes del adaptador completo.** Extensión local temporal + host que acepte un lote de ejemplo; medir chat nuevo, continuación, ediciones, regeneración, proyecto y reinicio. Documentar IDs disponibles, contenido invisible, cambios de DOM y si existe una ruta viable de instalación de extensión para un usuario no técnico. Esta prueba decide el diseño de Fase 3.

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
2. **Proyectos:** la asociación puede no estar presente o ser ambigua en la página. Se guardará solo cuando haya evidencia.
3. **Instalación:** extensión y host deben enlazarse sin terminal; IDs de tienda y firma pueden complicar el flujo.
4. **Privacidad:** la base inicial no tiene cifrado de aplicación; explicar protección real, exportación legible y borrado.
5. **Entorno de desarrollo:** `dotnet` e Inno Setup no están instalados en esta máquina; se necesitan para compilar y empaquetar, pero nunca en la máquina de los usuarios.

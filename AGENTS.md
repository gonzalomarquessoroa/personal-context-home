# Guía de trabajo

## Alcance actual

Leer README.md y docs/{roadmap,architecture,decisions,tasks}.md antes de cambiar comportamiento.
La dirección aprobada para Fase 2.1 es una ventana WPF mínima (.NET 10, Windows x64)
que use LocalStore, muestre ruta, estado y número de conversaciones, y se publique
por carpeta autocontenida. El instalador y el registro automático del puente con
la extensión son incrementos posteriores de Fase 2. No conectar la captura
experimental a SQLite. La aprobación del plan no autoriza implementarlo en una
tarea limitada a documentación o configuración.

## Pruebas reales

Desde la raíz, en PowerShell con SDK .NET 10 y Node para las pruebas del prototipo:

```powershell
$projectDotnet = ".\.tools\dotnet\dotnet.exe" # O "dotnet" si está en PATH.
& $projectDotnet restore PersonalContext.sln --configfile NuGet.Config
& $projectDotnet test PersonalContext.sln -c Release --no-restore

# Solo si se revisa el prototipo; el smoke necesita este host publicado.
& $projectDotnet publish src/PersonalContext.NativeHost/PersonalContext.NativeHost.csproj -c Release -r win-x64 --self-contained true -o .tools/native-host/publish
node scripts/probe-host-smoke.mjs
node scripts/probe-extension-fixture.mjs
node scripts/probe-worker-fixture.mjs
```

Comprobar el código de salida de cada comando; detenerse ante fallos. WPF y el
smoke del ejecutable Windows se verifican en Windows, no se dan por probados desde
Linux/WSL. Ejecutar `git diff --check` al terminar; distinguir fallos previos de
los introducidos. Conservar cambios ajenos y comunicar pruebas no ejecutadas.

## Datos y publicación

- Usar únicamente datos ficticios en pruebas. Corrupción, borrado, permisos y
  otras pruebas destructivas requieren perfiles desechables y bases temporales;
  nunca el perfil habitual ni su base en LOCALAPPDATA.
- No leer ni usar cookies, tokens de sesión, APIs privadas o endpoints internos
  de ChatGPT. Mantener el prototipo aislado y sus límites de cobertura explícitos.
- Antes de publicar, revisar archivos, artefactos e historial por conversaciones,
  bases, exportaciones, secretos y datos personales; .gitignore no basta.
  Verificar autoría con correo GitHub noreply. No hacer commit, push ni publicar
  sin autorización para esa acción.

Para procedimientos específicos: `.agents/skills/dotnet-wpf-build/SKILL.md` y
`.agents/skills/privacy-release-audit/SKILL.md`. El reviewer del proyecto está en
`.codex/agents/reviewer.toml` y trabaja sin modificar archivos.

---
name: dotnet-wpf-build
description: Compilar, probar y publicar el ejecutable WPF Windows de Personal Context Home; usar para validar cambios .NET o el paquete autocontenido, no para implementar funcionalidades.
---

# Validar .NET y WPF

1. Lee AGENTS.md para alcance y comandos de regresión. Comprueba los csproj y el
   SDK con `dotnet --info` o, en PowerShell, `& .\.tools\dotnet\dotnet.exe --info`.
   No descargues herramientas automáticamente. Detente si falta el entorno.
2. Ejecuta restore y test según AGENTS.md; verifica sus códigos de salida. Si no
   existe `src/PersonalContext.Desktop/PersonalContext.Desktop.csproj`, informa
   que Desktop está pendiente y no crees el proyecto como parte de esta skill.
3. Cuando exista Desktop, comprueba WinExe, net10.0-windows, UseWPF y referencia
   a Storage. Desde Windows y la raíz, publica con el SDK seleccionado en
   `$projectDotnet` según AGENTS.md:

   ```powershell
   & $projectDotnet publish src/PersonalContext.Desktop/PersonalContext.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o .tools/desktop/publish
   ```

4. Copia la carpeta publicada completa a una máquina o VM Windows x64 sin SDK
   ni runtime moderno de .NET. En un perfil desechable, abre el exe por doble
   clic y verifica ventana sin consola, base en LOCALAPPDATA, contador vacío y
   reapertura. Repite moviendo el paquete a una ruta con espacios. Verifica
   conservación de una base con datos ficticios y errores visibles ante fallos
   de apertura, sin sustitución automática de la base.
5. Informa SDK, comandos y resultados, ubicación del paquete y pruebas manuales
   realizadas o pendientes. Compilar o publicar no acredita el arranque ni la
   carga de SQLite en una máquina sin .NET. No amplíes el alcance para corregir
   requisitos todavía no implementados.

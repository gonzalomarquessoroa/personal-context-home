---
name: dotnet-wpf-build
description: Compilar, probar y publicar el ejecutable WPF Windows de Personal Context Home; usar para validar cambios .NET o el paquete autocontenido, no para implementar funcionalidades.
---

# Validar .NET y WPF

1. Lee AGENTS.md para alcance y comandos de regresión. Comprueba los csproj y el
   SDK con `dotnet --info` o, en PowerShell, `& .\.tools\dotnet\dotnet.exe --info`.
   Desde Bash en WSL usa `./.tools/dotnet/dotnet.exe --info`: es el SDK Windows;
   no uses sintaxis PowerShell ni `dotnet` Linux en esa shell.
   No descargues herramientas automáticamente. Detente si falta el entorno.
2. Ejecuta restore y test según AGENTS.md; verifica sus códigos de salida. Si no
   existe `src/PersonalContext.Desktop/PersonalContext.Desktop.csproj`, informa
   que Desktop está pendiente y no crees el proyecto como parte de esta skill.
3. Cuando exista Desktop, comprueba WinExe, net10.0-windows, UseWPF y referencia
   a Storage. Desde Windows y la raíz, publica con el SDK seleccionado en
   `$projectDotnet` según AGENTS.md:

   ```powershell
   $projectSourceRoot = (Get-Location).Path
   & $projectDotnet publish src/PersonalContext.Desktop/PersonalContext.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false "-p:PathMap=$projectSourceRoot=/_/" -o .tools/desktop/publish-clean
   ```

   Alternativa desde Bash en WSL, desde la raíz y con el SDK Windows:

   ```bash
   ./.tools/dotnet/dotnet.exe restore PersonalContext.sln --configfile NuGet.Config
   ./.tools/dotnet/dotnet.exe test PersonalContext.sln -c Release --no-restore
   projectSourceRoot=$(wslpath -w "$PWD")
   ./.tools/dotnet/dotnet.exe publish src/PersonalContext.Desktop/PersonalContext.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false "-p:PathMap=$projectSourceRoot=/_/" -o .tools/desktop/publish-clean
   ```

   Comprueba cada código de salida y detente ante fallos. Conserva los finales
   de línea existentes; no añadas cambios solo de CRLF. La publicación desde
   WSL con SDK Windows no acredita el smoke visual.

   Usa una salida nueva y vacía bajo `.tools/desktop/` en cada publicación;
   cambia `-o` si la indicada ya contiene archivos, porque publish no limpia
   sobrantes. El proyecto usa `CopyOutputSymbolsToPublishDirectory=false` y
   filtra los PDB de referencias tras `ComputeFilesToPublish`: solo afecta a
   publish, conservando símbolos en `bin`/`obj` para desarrollo y pruebas.
   El comando añade `PathMap` global para sustituir la raíz del código por
   `/_/` en las rutas de depuración de Desktop y bibliotecas referenciadas.
   No uses `DebugType=none` ni `DebugSymbols=false`: perderían esos símbolos.
   La compilación normal de desarrollo no requiere `PathMap`.
   Inspecciona el inventario completo y el contenido binario/texto (UTF-8 y
   UTF-16) para comprobar ausencia de PDB y rutas del perfil; informa solo
   cantidades y nombres de archivos, nunca las coincidencias privadas.
   No distribuyas la antigua `.tools/desktop/publish/`: contiene PDB privados.
   Tampoco distribuyas `.tools/desktop/publish-no-symbols/`: conserva rutas en DLL.

4. Copia la carpeta publicada completa a una máquina o VM Windows x64 sin SDK
   ni runtime moderno de .NET. En un perfil desechable, abre el exe por doble
   clic y verifica ventana sin consola, base en LOCALAPPDATA, contador vacío y
   reapertura. Repite moviendo el paquete a una ruta con espacios. Verifica
   conservación de una base con datos ficticios y errores visibles ante fallos
   de apertura, sin sustitución automática de la base.
   Usa solo datos ficticios y nunca la base del perfil Windows habitual. Si no
   hay perfil o VM desechable disponible, deja este smoke pendiente.
5. Informa SDK, comandos y resultados, ubicación del paquete y pruebas manuales
   realizadas o pendientes. Compilar o publicar no acredita el arranque ni la
   carga de SQLite en una máquina sin .NET. No amplíes el alcance para corregir
   requisitos todavía no implementados.

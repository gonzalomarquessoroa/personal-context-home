---
name: privacy-release-audit
description: Auditar archivos, artefactos e historial de Personal Context Home antes de publicar para detectar datos privados; solo inspección, sin commit, push, limpieza ni publicación.
---

# Auditar una publicación

Aplica las reglas de AGENTS.md. Esta skill produce un informe; no altera archivos
ni autoriza publicar. No imprimas valores de secretos o conversaciones: informa
solo ubicación, categoría y acción recomendada.

1. Delimita el candidato: `git status --short`, `git diff --stat`,
   `git diff --cached --stat`, `git ls-files` y
   `git ls-files --others --exclude-standard`. Revisa localmente ambos diffs y
   los archivos nuevos; incluye los no rastreados que se pretendan distribuir.
2. Busca bases y sus auxiliares, exportaciones, capturas, .env, certificados,
   claves, registros y rutas personales. Para buscar contenido sospechoso usa
   `rg -l` sobre los archivos candidatos, sin imprimir coincidencias. Inspecciona
   los casos necesarios localmente; distingue fixtures ficticias de datos reales.
3. Comprueba exclusiones con `git check-ignore --no-index -v -- <ruta>` y
   contrasta con `git ls-files`: un archivo ya rastreado sigue incluido aunque
   esté ignorado. Revisa aparte el inventario y contenido de la carpeta o ZIP
   de publicación; las exclusiones de Git no protegen el paquete.
4. Revisa rutas históricas con `git log --all --name-only --format=` y autoría
   con `git log --all --format='%h %an <%ae> %cn <%ce>'`. Examina las revisiones
   relevantes con `git show` sin volcar datos privados al informe. Para un
   repositorio aún no publicado, revisa todo el historial; para actualizaciones,
   revisa el rango que se publicará y declara cualquier límite de cobertura.
   Comprueba también `git config --get user.email` para futuros commits.
5. Entrega hallazgos por archivo o commit, cobertura de archivos/historial/paquete
   y verificaciones pendientes. Si hay datos privados, señala qué impide publicar
   y propone corrección; no borres archivos ni reescribas historial. No declares
   una auditoría completa a partir de búsquedas automáticas o .gitignore.

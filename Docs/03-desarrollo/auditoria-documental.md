# Auditoría documental · noviembre de 2026

[← Desarrollo](README.md)

## Qué revisamos

Contrastamos portada, índices, informes históricos, referencias especializadas, `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset`, clases de estados, modelos de perfil, rutas del servidor de feria y releases publicadas.

## Correcciones aplicadas

| Hallazgo | Tratamiento |
| --- | --- |
| Índice con cuatro áreas sin sección de GDD/manuales | Crear cinco áreas navegables y una página central de diseño de juego. |
| README apuntaba a Windows 1.0.0 / Android beta 2 | Actualizar enlaces a Windows 1.0.1 / Android 1.0.1 beta 1. |
| Plantilla genérica hablaba de «clientes», IA y presupuestos empresariales | Retirar esas secciones de la navegación principal; describir el videojuego y su origen universitario. |
| Títulos y capítulos duplicados | Consolidar la entrada en capítulos por área y enlazar referencias especializadas sin copiarlas. |
| Riesgo de confundir JSON con SQL | Crear modelo conceptual JSON y diagrama ER **exclusivo** para SQLite de feria. |
| Tutorial jugable existente en fuentes, pero no en build | Diferenciar el cómic activo de la escena `ZonaTutorial` aislada. |
| Servidor con API más amplia que el alcance validado | Explicitar API implementada vs. leaderboard de host realmente presentado. |
| Informes del curso mezclados con código vigente | Conservación intacta en `Docs/reports/` con índice académico propio. |

## Documentación anterior conservada por compatibilidad

Los capítulos numerados anteriores (`00` a `18`) y los documentos especializados de la raíz `Docs/` no se borran en esta propuesta. Pueden contener enlaces entrantes desde el historial y detalles útiles; **la navegación principal deja de recomendarlos como capítulos canónicos** cuando repiten material o introducen categorías poco pertinentes.

Antes de una limpieza definitiva, verificar enlaces entrantes, referencias cruzadas y valor de archivo. En particular, los apartados económicos con precios genéricos de Supabase/Vercel/Gemini no se deben presentar como costos medidos de este videojuego.

## Límites de la auditoría

La existencia de scripts, escenas y tests no implica que se haya ejecutado todo en esta revisión. Los diagramas de estados describen el diseño respaldado por enums y documentación; una matriz exacta de transiciones requiere verificar cada controlador y sus guardas. Las validaciones de runtime y rendimiento deben hacerse con el editor y dispositivos correspondientes.

No editamos informes entregados, ni asignamos nuevas motivaciones, responsabilidades individuales o licencias que no estén confirmadas por el equipo.

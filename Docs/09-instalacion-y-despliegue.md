# 09 — Puesta en marcha y distribución

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** técnica.

## Puesta en marcha y distribución por plataforma

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

**Destinos:** Windows estable + Android beta; release final condicionada a validación.

Editor declarado en ProjectVersion.txt: `m_EditorVersion: 6000.3.11f1`. Los paquetes se resuelven desde Packages/manifest.json y packages-lock.json.

Proyecto Unity: abrir la raíz desde Unity Hub con el editor exacto, instalar sólo el módulo del target aprobado y seguir escenas/pruebas/build documentados. Conservar GUID y `.meta`.

### Secuencia de puesta en marcha

1. Preparar el runtime/editor y la configuración que indiquen las fuentes enlazadas; usar valores de prueba y restaurar el lockfile apropiado.
2. Ejecutar la modalidad local y sus comprobaciones existentes desde la carpeta indicada por esa guía. No confundir preview web con el runtime nativo o montaje físico.
3. Construir/instalar el artefacto de ese target y comprobar recorrido principal, error relevante y reapertura/actualización antes de declararlo entregado.

Los comandos se enumeran desde archivos presentes; esta actualización documental no los ejecuta de nuevo ni certifica sus resultados. Estado de pruebas y CI se informa por revisión/fecha, separado de capacidades propuestas.

### Datos, servicios y distribución

Datos iniciales en Resources/PersistentDbSeeds y progreso en Application.persistentDataPath/db. JSON local para perfil, récords y catálogo; SQLite del servidor de feria es otro almacén limitado al host. No hay una base compartida de todos los jugadores ni sincronización integral de compras.

Para APK: application ID estable, versión/código creciente, firma privada preservada, permisos y prueba en dispositivo; adjuntar APK y SHA-256 a la release. Para escritorio: distinguir portable/instalador y dependencias. Para web: base/rutas, servidor/servicios y cambio de origen. Para firmware: dispositivo, cableado, compilación y prueba física separados. Aplicar sólo los targets aprobados.

GitHub Releases no aloja el backend ni convierte un ZIP de fuentes en instalador. No hacer push como parte de una revisión documental: en proyectos con publicación automática desde main, ese push puede generar una nueva APK o despliegue.

### Recuperación

Conservar el artefacto anterior y su procedencia. Retroceder código no revierte automáticamente datos o migraciones. Revisar compatibilidad de guardado/snapshot y restauración antes de instalar una versión antigua. La aceptación debe identificar archivo/URL, commit, target, fecha y limitaciones.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

<!-- DOCUMENTACION:evidencia-ci-2026-10-29:BEGIN -->

## Estado remoto consultado el 29 de octubre de 2026

Consulta para el commit `3feadba8f146cc49dd96dd56a516966cb55c8824`. Se leyó GitHub Actions; no se ejecutó de nuevo la aplicación ni se repitieron sus pruebas.

No se obtuvo una ejecución de workflow para este SHA en la consulta. Esto no demuestra ausencia de pruebas ni valida el runtime.

Releases consultadas (no una comprobación nueva de instalación):

| Tag                                                                                                                    | Canal                          | Adjuntos identificados                                                                                                      |
| ---------------------------------------------------------------------------------------------------------------------- | ------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| [windows-v1.0.0](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/windows-v1.0.0)               | Publicada sin marca prerelease | build-info.json, RELEASE_NOTES.md, SHA256SUMS.txt, source-info.json, SquidInkPulse-Windows-x64-v1.0.0.zip                   |
| [android-v1.0.0-beta.1](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases/tag/android-v1.0.0-beta.1) | Prerelease                     | RELEASE_NOTES.md, SHA256SUMS.txt, source-info.json, SOURCE-TimeManager.patch, SquidInkPulse-Android-arm64-v1.0.0-beta.1.apk |

<!-- DOCUMENTACION:evidencia-ci-2026-10-29:END -->

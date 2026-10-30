# 03 — Recorrido y estados

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** general / técnica.

## Recorrido operativo y estados

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

1. Abrir menú y revisar controles.
2. Iniciar run y aprender relación riesgo/carga.
3. Usar Ink-Pulse, gadgets y tienda de run.
4. Transitar zonas y resolver la sesión.
5. Consultar resultado/perfil y continuar o cerrar conservando progreso.

### Confirmación y resultado

Graze y carga habilitan Ink-Pulse; no se confunden con el daño/colisión.

El leaderboard de feria no acredita sincronización de progreso entre equipos.

En formularios/operaciones, distinguir captura, revisión, confirmación, resultado y fallo. Cancelar una previsualización no debe anunciar un cambio no confirmado; cuando existe una operación persistente, su recuperación sigue el contrato específico. En juegos, las fases de simulación se definen en el núcleo y la vista refleja esas transiciones.

### Criterio de aceptación del recorrido

Jugabilidad completa, comprensión de Ink-Pulse, estabilidad de perfil y rendimiento por target; ventas/retención sólo se estiman tras datos de uso.

Validar inicio, camino principal, error relevante, cancelación y reapertura según la plataforma; registrar también los escenarios aún no ejecutados. Para Squid Ink-Pulse, atender especialmente: referencias de escena/prefab y estado al cambiar zonas.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

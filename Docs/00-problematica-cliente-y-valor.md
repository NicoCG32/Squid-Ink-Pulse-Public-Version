# 00 — Problemática, cliente y solución

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** general.

## Síntesis general — Squid Ink-Pulse

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

### Problemática

El objetivo de diseño es ofrecer un endless runner donde acercarse al peligro aporte una ventaja deliberada y no sólo castigo. Además del reto lúdico, el proyecto académico exige integrar reglas, escenas, persistencia, interfaz y distribución en una experiencia completa.

### Cliente, usuario y beneficiario

| Perfil                  | Contexto                                           | Necesidad                                           |
| ----------------------- | -------------------------------------------------- | --------------------------------------------------- |
| Jugador                 | Persona que busca una sesión de habilidad y riesgo | Movimiento, lectura del peligro y uso de Ink-Pulse  |
| Equipo académico        | Desarrolladores de la entrega                      | Integración verificable y presentación del producto |
| Público de distribución | Usuario Windows o Android beta                     | Instalar y entender límites del canal               |

### Solución y límites

Endless runner 2D submarino: el graze carga Ink-Pulse y abre una ventaja temporal; zonas, enemigos, bosses, portales y economía local sostienen progresión. La base funciona sin servicios; feria es un add-on opcional y Android requiere controles/UI/ciclo de vida propios.

### Por qué corresponde al problema

| Fricción / necesidad          | Mecanismo de la solución                                               |
| ----------------------------- | ---------------------------------------------------------------------- |
| Riesgo sin decisión           | Graze e Ink-Pulse convierten cercanía al peligro en un recurso         |
| Sesiones sin continuidad      | Perfil, mejoras y récords conservan progreso local                     |
| Entrega difícil de reproducir | Escenas, editor, tests y paquetes completos explicitan la distribución |

### Validación pendiente o continua

Jugabilidad completa, comprensión de Ink-Pulse, estabilidad de perfil y rendimiento por target; ventas/retención sólo se estiman tras datos de uso.

La motivación del creador se presenta en [16-motivacion-y-contexto.md](16-motivacion-y-contexto.md); el análisis de costos y proyección se separa en [18-analisis-economico-clp-usd.md](18-analisis-economico-clp-usd.md). Afinidad personal, un prototipo o cercanía con una organización no equivalen a demanda/venta demostrada.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

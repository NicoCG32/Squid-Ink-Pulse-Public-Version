# 00 — Problemática, cliente y solución

## Síntesis general — Squid Ink-Pulse

**Disponibilidad:** Juego local con versión Windows y beta Android. La adaptación móvil continúa con ajustes de controles, interfaz, suspensión y rendimiento.

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

Puedes conocer el origen del proyecto en [16-motivacion-y-contexto.md](16-motivacion-y-contexto.md); el análisis de costos y proyección se separa en [18-analisis-economico-clp-usd.md](18-analisis-economico-clp-usd.md).

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

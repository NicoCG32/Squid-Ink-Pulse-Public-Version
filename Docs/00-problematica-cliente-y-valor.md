# 00 — Problemática, cliente y solución

Desarrollé Squid Ink-Pulse junto al equipo de Yeco Works en Proyecto Integrador Programación Avanzada de la Universidad Católica del Norte. Es un endless runner submarino donde acercarse al peligro carga Ink-Pulse; incluye zonas, jefes, tiendas y progreso local.

**Disponibilidad:** Juego local con versión Windows y beta Android. La adaptación móvil continúa con ajustes de controles, interfaz, suspensión y rendimiento.

## Problemática

El objetivo de diseño es ofrecer un endless runner donde acercarse al peligro aporte una ventaja deliberada y no sólo castigo. Además del reto lúdico, el proyecto académico exige integrar reglas, escenas, persistencia, interfaz y distribución en una experiencia completa.

## Cliente, usuario y beneficiario

| Perfil                  | Contexto                                           | Necesidad                                           |
| ----------------------- | -------------------------------------------------- | --------------------------------------------------- |
| Jugador                 | Persona que busca una sesión de habilidad y riesgo | Movimiento, lectura del peligro y uso de Ink-Pulse  |
| Equipo académico        | Desarrolladores de la entrega                      | Integración verificable y presentación del producto |
| Público de distribución | Usuario Windows o Android beta                     | Instalar y entender límites del canal               |

## Solución y límites

Endless runner 2D submarino: el graze carga Ink-Pulse y abre una ventaja temporal; zonas, enemigos, bosses, portales y economía local sostienen progresión. La base funciona sin servicios; feria es un add-on opcional y Android requiere controles/UI/ciclo de vida propios.

## Por qué corresponde al problema

| Fricción / necesidad          | Mecanismo de la solución                                               |
| ----------------------------- | ---------------------------------------------------------------------- |
| Riesgo sin decisión           | Graze e Ink-Pulse convierten cercanía al peligro en un recurso         |
| Sesiones sin continuidad      | Perfil, mejoras y récords conservan progreso local                     |
| Entrega difícil de reproducir | Escenas, editor, tests y paquetes completos explicitan la distribución |

## Cómo quiero evaluar la propuesta

Quiero que una persona pueda aprender la mecánica, completar una partida y retomar su progreso. En Android eso también exige comprobar controles, interrupciones y rendimiento en el dispositivo, además de las pruebas del Editor.

[Motivación y contexto](16-motivacion-y-contexto.md) · [Proyección](15-escalabilidad-y-nuevos-mercados.md) · [Estimaciones comerciales](18-analisis-economico-clp-usd.md).

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

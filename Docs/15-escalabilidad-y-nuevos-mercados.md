# 15 — Justificación, proyección y escalabilidad

## Justificación y proyección

Me interesa la decisión de acercarse al peligro para cargar Ink-Pulse y convertir ese riesgo en una ventaja. Seguir trabajando en el juego y en su adaptación móvil me permite cuidar esa experiencia y llevarla a otra forma de jugar.

### Ajuste entre problema y solución

| Necesidad                     | Razón del diseño                                                       |
| ----------------------------- | ---------------------------------------------------------------------- |
| Riesgo sin decisión           | Graze e Ink-Pulse convierten cercanía al peligro en un recurso         |
| Sesiones sin continuidad      | Perfil, mejoras y récords conservan progreso local                     |
| Entrega difícil de reproducir | Escenas, editor, tests y paquetes completos explicitan la distribución |

Jugabilidad completa, comprensión de Ink-Pulse, estabilidad de perfil y rendimiento por target; ventas/retención sólo se estiman tras datos de uso.

Quiero que una persona pueda aprender la mecánica, completar una partida y retomar su progreso. En Android eso también exige comprobar controles, interrupciones y rendimiento en el dispositivo, además de las pruebas del Editor.

### Qué se puede abstraer y qué no

La mecánica de riesgo y recompensa puede inspirar nuevos escenarios del juego. El perfil local, las mejoras y la composición por zonas ofrecen una base de trabajo; cualquier nueva distribución debe cuidar la experiencia original y los derechos de todo el equipo.

### Posibles líneas de evolución

Son caminos que me gustaría explorar después del alcance actual.

- **Nuevos escenarios del juego:** Podrían explorar variaciones de riesgo y recompensa sin perder la lectura de Ink-Pulse.
- **Muestras y eventos:** El modo de feria puede acompañar partidas públicas con una configuración separada del juego local.
- **Sesiones móviles cortas:** La adaptación permite revisar otra duración y forma de jugar, atendiendo a controles y ciclo de vida.

### Límites y trade-offs

- Referencias de escena/prefab y estado al cambiar zonas
- Compatibilidad de perfiles y compras
- Memoria, rendimiento y suspensión móvil
- Derechos de recursos de equipo y mezcla entre rama de desarrollo/release

### Prioridad de inversión

| Prioridad | Iniciativa                                                    | Qué quiero conseguir                                          |
| --------- | ------------------------------------------------------------- | ------------------------------------------------------------- |
| Alta      | Completar la evaluación móvil en dispositivo                  | Revisar partidas, interrupciones y rendimiento prolongado.    |
| Media     | Conservar compatibilidad y distribución por plataforma        | Actualizar el juego sin perder progreso.                      |
| Futura    | Acordar continuidad y posibles usos comerciales con el equipo | Definir contenido, autoría y sostenimiento de forma conjunta. |

Las [estimaciones comerciales y de operación](18-analisis-economico-clp-usd.md) permiten conversar sobre estos alcances con referencias para Chile y el extranjero.

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

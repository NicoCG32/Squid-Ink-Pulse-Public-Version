# 15 — Justificación, proyección y escalabilidad

## Justificación y proyección

### Ajuste entre problema y solución

| Necesidad                     | Razón del diseño                                                       |
| ----------------------------- | ---------------------------------------------------------------------- |
| Riesgo sin decisión           | Graze e Ink-Pulse convierten cercanía al peligro en un recurso         |
| Sesiones sin continuidad      | Perfil, mejoras y récords conservan progreso local                     |
| Entrega difícil de reproducir | Escenas, editor, tests y paquetes completos explicitan la distribución |

Jugabilidad completa, comprensión de Ink-Pulse, estabilidad de perfil y rendimiento por target; ventas/retención sólo se estiman tras datos de uso.

### Qué se puede abstraer y qué no

Las entidades, reglas y fronteras de [04-base-de-datos.md](04-base-de-datos.md) permiten evaluar reutilización. Cambiar un nombre o una categoría no prueba compatibilidad con otro sector: datos, permisos, reglas, operación y costos deben validarse de nuevo.

### Posibles líneas de evolución

- Distribución independiente de juego 2D
- Eventos y muestras académicas
- Otros escenarios con mecánica riesgo/recompensa
- Experiencias móviles de sesiones cortas

### Límites y trade-offs

- Referencias de escena/prefab y estado al cambiar zonas
- Compatibilidad de perfiles y compras
- Memoria, rendimiento y suspensión móvil
- Derechos de recursos de equipo y mezcla entre rama de desarrollo/release

### Prioridad de inversión

| Prioridad   | Iniciativa                                               | Impacto a comprobar                                                                                                                                 | Criterio económico                                                    |
| ----------- | -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Alta        | Cierre de un gate móvil con run física, UI y rendimiento | Jugabilidad completa, comprensión de Ink-Pulse, estabilidad de perfil y rendimiento por target; ventas/retención sólo se estiman tras datos de uso. | Rango de esfuerzo y costo en capítulo 18; no presupuesto comprometido |
| Media       | Reproducción, soporte y recuperación por plataforma      | Menos fallos de entrega y pérdida de datos                                                                                                          | Costo recurrente separado de implementación                           |
| Estratégica | Validar una extensión/forma de sostenimiento             | Uso y aceptación fuera del contexto inicial                                                                                                         | Investigar costos, demanda y condiciones antes de monetizar           |

Las cifras CLP/USD, supuestos, sensibilidad y retorno se presentan en [18-analisis-economico-clp-usd.md](18-analisis-economico-clp-usd.md); no son una valoración comercial del repositorio.

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

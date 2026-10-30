# 17 — Etapas y metodología

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** desarrollo del proyecto.

## Desarrollo del proyecto: etapas y método

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

El README describe desarrollo de equipo con metodología Scrum. Los informes académicos documentan etapas de esa entrega; no se inventan sprints, horas o métricas que no consten. El mantenimiento actual y el port se organizan por incrementos/puertas de prueba sobre una base cerrada.

### Etapas reconstruidas de fuentes

| Etapa              | Evidencia / alcance                                                  |
| ------------------ | -------------------------------------------------------------------- |
| Proyecto académico | Diseño e integración de la experiencia base                          |
| Entrega Windows    | Juego local y documentos de cierre del curso                         |
| Mantenimiento      | Contratos, tests y distribución con procedencia                      |
| Port Android       | Controles, menús, suspensión y rendimiento; beta no equivale a final |

El cuadro distingue hechos documentados y pendientes. Un archivo de planificación describe intención; un commit confirma contenido de código/documentación; una prueba/artefacto con procedencia aporta evidencia de comportamiento. No son intercambiables.

### Ciclo de trabajo y decisión

1. Caracterizar una necesidad y un escenario observable.
2. Revisar contratos, datos y límites del alcance.
3. Implementar o prototipar el incremento apropiado a la etapa.
4. Verificar el recorrido y sus errores relevantes; conservar resultados negativos.
5. Decidir continuar, corregir, limitar o posponer y actualizar documentación vigente.

Este ciclo es una propuesta de continuidad compatible con el método descrito arriba; no se afirma que todas las etapas históricas se ejecutaran con idéntica ceremonia o duración.

### Cierre y continuidad

La entrega identifica fuente, plataforma, límites, guía de uso e integridad de datos/artefactos. Los informes académicos conservan su etapa, cuando aplican, y no se actualizan como si fueran manuales del runtime. La proyección no obliga a desarrollar extensiones antes de validar el núcleo.

Justificación y prioridades: [15-escalabilidad-y-nuevos-mercados.md](15-escalabilidad-y-nuevos-mercados.md). Esfuerzo futuro y operación: [18-analisis-economico-clp-usd.md](18-analisis-economico-clp-usd.md).

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

# Índice de documentación

<!-- DOCUMENTACION:indice-niveles-2026-10-29:BEGIN -->

## Documentación por cuatro niveles — 2026-10-29

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

| Nivel                      | Pregunta principal                                                    | Lectura                                                  |
| -------------------------- | --------------------------------------------------------------------- | -------------------------------------------------------- |
| 1. General                 | Qué problema, para quién, qué solución y por qué lo eligió el creador | 00, 02, 03 y 16                                          |
| 2. Técnica                 | Cómo funciona, entidades, persistencia y ejecución                    | 01, 04, 05, 06, 07, 08 y 09                              |
| 3. Desarrollo del proyecto | Cómo se llegó, por qué corresponde y qué cuesta continuar             | 17, 15 y 18                                              |
| 4. Académica/formativa     | Qué se entregó en un curso y a qué etapa pertenece                    | [Documentación académica/formativa](academica/README.md) |

### Rutas por lector

- **Dirección/cliente/responsable:** 00 → 16 → 01 → 15 → 18.
- **Desarrollo/técnica:** 01 → 04 → 05 → 06 → 09; complementar con las guías especializadas conservadas.
- **Producto/operaciones:** 00 → 02 → 03 → 07 → 08 → 17.
- **Académica/formativa:** contexto de 16 y catálogo del nivel 4 cuando corresponda; contrastar luego con la arquitectura vigente.

| Capítulo | Documento                                                                          | Nivel                   |
| -------- | ---------------------------------------------------------------------------------- | ----------------------- |
| 00       | [Problemática, cliente y solución](00-problematica-cliente-y-valor.md)             | general                 |
| 01       | [Arquitectura por niveles](01-arquitectura-general.md)                             | técnica                 |
| 02       | [Catálogo funcional](02-funcionalidades.md)                                        | general / técnica       |
| 03       | [Recorrido y estados](03-flujo-de-usuario.md)                                      | general / técnica       |
| 04       | [Dominio, entidades y persistencia](04-base-de-datos.md)                           | técnica                 |
| 05       | [Reglas y núcleo](05-motor-o-nucleo-de-negocio.md)                                 | técnica                 |
| 06       | [Integraciones y asistencia](06-capa-de-ia-o-analisis.md)                          | técnica                 |
| 07       | [Diseño y accesibilidad](07-diseno-e-identidad.md)                                 | técnica / producto      |
| 08       | [Salidas y entregables](08-exportacion-e-informes.md)                              | técnica                 |
| 09       | [Puesta en marcha y distribución](09-instalacion-y-despliegue.md)                  | técnica                 |
| 15       | [Justificación, proyección y escalabilidad](15-escalabilidad-y-nuevos-mercados.md) | desarrollo del proyecto |
| 16       | [Motivación personal y contexto](16-motivacion-y-contexto.md)                      | general                 |
| 17       | [Etapas y metodología](17-desarrollo-y-metodologia.md)                             | desarrollo del proyecto |
| 18       | [Escenarios económicos CLP / USD](18-analisis-economico-clp-usd.md)                | desarrollo del proyecto |

Los nuevos capítulos complementan las fuentes especializadas existentes; no reemplazan contratos detallados por resúmenes. Los originales académicos se conservan intactos. La documentación del desarrollo describe etapas/metodología del producto y no contiene instrucciones o conversaciones internas.
<!-- DOCUMENTACION:indice-niveles-2026-10-29:END -->

Este directorio organiza la documentación técnica vigente de Squid Ink-Pulse como juego desarrollado, jugable y cerrado en su producto base. Los informes de `Docs/reports/` conservan caracter histórico y no se reescriben para reflejar cada cambio de implementación.

## Base del proyecto

- [Distribution.md](Distribution.md) - descarga por plataforma, ZIP Windows, APK beta, checksums y validación de releases.

- [ProjectOverview.md](ProjectOverview.md) - vision general, pilares y estado del producto base.
- [SoftwareArchitecture.md](SoftwareArchitecture.md) - contrato de capas, dependencias, estados, especializaciones y excepciones.
- [ProjectStructure.md](ProjectStructure.md) - organizacion de carpetas y criterios.
- [RuntimeHierarchyAudit.md](RuntimeHierarchyAudit.md) - scripts esperados por nodo, prefab y responsabilidad.
- [ZonaEpipelagica.md](ZonaEpipelagica.md) - estructura de escena, managers y jerarquía runtime.
- [AssetFlow.md](AssetFlow.md) - entrada y uso de audio, arte, prefabs y animaciones.
- [WorldScale.md](WorldScale.md) - escala canonica entre pixeles, unidades de mundo y boundaries.
- [AnimationStandards.md](AnimationStandards.md) - reglas y pautas de animaciones runtime.
- [Testing.md](Testing.md) - base mínima de pruebas EditMode y ejecución batch.
- [MobilePort.md](MobilePort.md) - contrato, alcance y matriz de validación del port móvil Android.
- [MobileBaseline.md](MobileBaseline.md) - referencia Windows reproducible anterior a los cambios del port.
- [AndroidBuild.md](AndroidBuild.md) - generación reproducible, instalación ADB y evidencia del APK Development.
- [Input.md](Input.md) - acciones semánticas, bindings de escritorio y separación segura entre UI y gameplay touch.

## Sistemas de juego

- [CoreSystems.md](CoreSystems.md) - sesión, progresión, flujo de escenas y persistencia.
- [PersistentProfile.md](PersistentProfile.md) - base JSON local `db`: catalogo de desbloqueables, perfil, records, leaderboard y script de limpieza de persistencia.
- [FairServer.md](FairServer.md) - add-on opcional de feria con servidor LAN, SQLite en el host y leaderboard web; no reemplaza la persistencia local.
- [FairEventSetupGuide.md](FairEventSetupGuide.md) - guía operativa para probar el add-on de feria, interpretar warnings de host y visualizar el leaderboard.
- [GameplaySystems.md](GameplaySystems.md) - movimiento, Ink-Pulse, graze, colisiones, camarones y gadgets.
- [EnemiesAndBosses.md](EnemiesAndBosses.md) - spawn, enemigos, SS Carnage y boss abisal.
- [Portals.md](Portals.md) - portales entre zonas y reglas de carga.
- [LoreComics.md](LoreComics.md) - viñetas narrativas de inicio, portales y derrota.
- [ZoneLighting.md](ZoneLighting.md) - oscuridad de `ZonaAbisopelagica` y light graze visual independiente.
- [WorldAndCamera.md](WorldAndCamera.md) - cámara, boundaries, parallax y limpieza fuera de pantalla.
- [UiAndMenu.md](UiAndMenu.md) - menus, pausa, game over, tienda y HUD.

- [Tutorial.md](Tutorial.md) - comic tutorial activo y tutorial jugable pendiente fuera del build.

- [SceneCompositionValidation.md](SceneCompositionValidation.md) - validacion Editor/batch de escenas, prefabs canonicos y busquedas globales restantes.

## Estado del producto base y evolución

- [StateMachines.md](StateMachines.md) - estados implementados y extensiones previstas.
- [ROADMAP.md](ROADMAP.md) - estado del producto base, add-on de feria y actualizaciones posteriores posibles.
- [ProjectReport.md](ProjectReport.md) - resumen breve del informe histórico.
- [reports/InformeSquidInkPulse.md](reports/InformeSquidInkPulse.md) - informe histórico completo.

<!-- INDICE:uso-y-licencia -->

## Uso, distribución y condiciones

- [Entrada y descargas](../README.md).
- [Licencia, créditos y reutilización](../LICENSING.md).
- [Referencia ampliada de uso](Referencia-de-uso.md).

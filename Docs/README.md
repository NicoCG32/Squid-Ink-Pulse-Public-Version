# Índice de documentación

Este directorio organiza la documentación técnica vigente de Squid Ink-Pulse como juego desarrollado, jugable y cerrado en su producto base. Los informes de `Docs/reports/` conservan caracter histórico y no se reescriben para reflejar cada cambio de implementación.

## Base del proyecto

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

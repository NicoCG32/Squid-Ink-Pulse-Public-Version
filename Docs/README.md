# Indice de documentacion

Este directorio organiza la documentacion viva del proyecto. Los informes de `Docs/Reports/` conservan caracter historico y no se reescriben para reflejar cada cambio de implementacion.

## Base del proyecto

- [ProjectOverview.md](ProjectOverview.md) - vision general, pilares y estado actual.
- [ProjectStructure.md](ProjectStructure.md) - organizacion de carpetas y criterios.
- [RuntimeHierarchyAudit.md](RuntimeHierarchyAudit.md) - scripts esperados por nodo, prefab y responsabilidad.
- [ZonaEpipelagica.md](ZonaEpipelagica.md) - estructura de escena, managers y jerarquia runtime.
- [AssetFlow.md](AssetFlow.md) - entrada y uso de audio, arte, prefabs y animaciones.
- [WorldScale.md](WorldScale.md) - escala canonica entre pixeles, unidades de mundo y boundaries.
- [AnimationStandards.md](AnimationStandards.md) - reglas y checklist de animaciones runtime.

## Sistemas de juego

- [CoreSystems.md](CoreSystems.md) - sesion, progresion, flujo de escenas y persistencia.
- [GameplaySystems.md](GameplaySystems.md) - movimiento, Ink-Pulse, graze, colisiones, camarones y gadgets.
- [EnemiesAndBosses.md](EnemiesAndBosses.md) - spawn, enemigos y SS Carnage.
- [Portals.md](Portals.md) - portales entre zonas y reglas de carga.
- [ZoneLighting.md](ZoneLighting.md) - oscuridad de `ZonaAbisopelagica` y light graze visual independiente.
- [WorldAndCamera.md](WorldAndCamera.md) - camara, boundaries, parallax y limpieza fuera de pantalla.
- [UiAndMenu.md](UiAndMenu.md) - menus, pausa, game over, tienda y HUD.

## Diseno y futuro

- [StateMachines.md](StateMachines.md) - estados implementados y planificados.
- [QATester.md](QATester.md) - parametros ajustables y checklist de pruebas.
- [ROADMAP.md](ROADMAP.md) - prioridades y sistemas futuros.
- [Tutorial.md](Tutorial.md) - secuencia prevista para `ZonaTutorial` y menus globales futuros.
- [ProjectReport.md](ProjectReport.md) - resumen breve del informe historico.
- [Reports/InformeSquidInkPulse.md](Reports/InformeSquidInkPulse.md) - informe historico completo.

## Regla general

- Si algo cambia comportamiento del juego, debe vivir en un documento de sistema.
- Si algo describe flujo de produccion o contenido, debe quedar separado de la logica de codigo.
- Si una seccion crece demasiado, se divide en otro `.md`.
- Si una implementacion cambia el contrato de escena, actualizar `RuntimeHierarchyAudit.md`, `WorldAndCamera.md` y `QATester.md`.

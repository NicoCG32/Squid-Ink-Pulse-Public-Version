# Indice de documentacion de entrega

Este directorio organiza la documentacion tecnica de la entrega final. Los informes de `Docs/Reports/` conservan caracter historico y no se reescriben para reflejar cada cambio de implementacion.

## Base del proyecto

- [ProjectOverview.md](ProjectOverview.md) - vision general, pilares y estado de entrega.
- [SoftwareArchitecture.md](SoftwareArchitecture.md) - contrato de capas, dependencias, estados, especializaciones y excepciones.
- [ProjectStructure.md](ProjectStructure.md) - organizacion de carpetas y criterios.
- [RuntimeHierarchyAudit.md](RuntimeHierarchyAudit.md) - scripts esperados por nodo, prefab y responsabilidad.
- [ZonaEpipelagica.md](ZonaEpipelagica.md) - estructura de escena, managers y jerarquia runtime.
- [AssetFlow.md](AssetFlow.md) - entrada y uso de audio, arte, prefabs y animaciones.
- [WorldScale.md](WorldScale.md) - escala canonica entre pixeles, unidades de mundo y boundaries.
- [AnimationStandards.md](AnimationStandards.md) - reglas y checklist de animaciones runtime.

## Sistemas de juego

- [CoreSystems.md](CoreSystems.md) - sesion, progresion, flujo de escenas y persistencia.
- [PersistentProfile.md](PersistentProfile.md) - base JSON local `db`: catalogo de desbloqueables, perfil, records, leaderboard y script de limpieza de persistencia.
- [FairServer.md](FairServer.md) - servidor LAN MVP de feria con SQLite, snapshots y ranking web.
- [FairEventSetupGuide.md](FairEventSetupGuide.md) - guia operativa paso a paso para generar build, levantar servidor y conectar 3 o 4 PCs desde cero.
- [GameplaySystems.md](GameplaySystems.md) - movimiento, Ink-Pulse, graze, colisiones, camarones y gadgets.
- [EnemiesAndBosses.md](EnemiesAndBosses.md) - spawn, enemigos, SS Carnage y boss abisal.
- [Portals.md](Portals.md) - portales entre zonas y reglas de carga.
- [LoreComics.md](LoreComics.md) - vinetas narrativas de inicio, portales y derrota.
- [ZoneLighting.md](ZoneLighting.md) - oscuridad de `ZonaAbisopelagica` y light graze visual independiente.
- [WorldAndCamera.md](WorldAndCamera.md) - camara, boundaries, parallax y limpieza fuera de pantalla.
- [UiAndMenu.md](UiAndMenu.md) - menus, pausa, game over, tienda y HUD.

## Validacion y continuidad

- [StateMachines.md](StateMachines.md) - estados implementados y extensiones previstas.
- [QATester.md](QATester.md) - parametros ajustables y checklist de pruebas.
- [ROADMAP.md](ROADMAP.md) - alcance de entrega y continuidad recomendada.
- [Tutorial.md](Tutorial.md) - secuencia de `ZonaTutorial` y puntos de extension.
- [ProjectReport.md](ProjectReport.md) - resumen breve del informe historico.
- [Reports/InformeSquidInkPulse.md](Reports/InformeSquidInkPulse.md) - informe historico completo.

## Regla general

- Si algo cambia comportamiento del juego, debe vivir en un documento de sistema.
- Si algo describe flujo de produccion o contenido, debe quedar separado de la logica de codigo.
- Si una seccion crece demasiado, se divide en otro `.md`.
- Si una implementacion cambia el contrato de escena, actualizar `RuntimeHierarchyAudit.md`, `WorldAndCamera.md` y `QATester.md`.

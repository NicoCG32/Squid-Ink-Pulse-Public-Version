# Resumen del proyecto

## Que es

Squid Ink-Pulse es un endless runner 2D en Unity donde el jugador controla a un calamar bebe que avanza de forma continua en un entorno submarino hostil. La idea central no es evitar el peligro por completo, sino convertirlo en una fuente de ventaja mediante Ink-Pulse.

## Pilares

- Riesgo y recompensa: acercarse a amenazas carga el recurso principal.
- Ritmo constante: el juego solo se detiene por estados globales o overlays temporales controlados.
- Claridad visual: el entorno debe leerse rapido incluso cuando la densidad sube.
- Rejugabilidad: progresion de run, bosses, tienda, gadgets y portales modifican cada intento.

## Estado tecnico actual

- Movimiento del jugador limitado por `PlayerBoundaries`.
- Jugador canonico como `Assets/Content/Prefabs/Player/BabySquid.prefab`, instanciado en zonas jugables como `Squid`.
- Camara limitada por `CameraBoundaries`.
- Spawn por perfiles de enemigo, con tags centralizados.
- SS Carnage integrado con progresion y red ajustada a boundaries.
- Tienda temporal mediante `DealerFish` e `InGameShopManager`.
- Gadgets comprables con inventario runtime no stackable.
- Portales entre `ZonaEpipelagica` y `ZonaExe`.
- Gadgets e Ink-Pulse persisten entre portales y se reinician al entrar en Game Over.
- `ZonaExe` tiene oscuridad ambiental por overlay y `LightGraze` visual independiente del graze de Ink-Pulse.

## Estructura general

- `Assets/Implementation/Code/`: logica de juego, UI, boss, camara, spawn y utilidades.
- `Assets/Content/`: arte, audio, prefabs y animaciones runtime.
- `Assets/Scenes/`: escenas del juego.
- `Docs/`: documentacion tecnica, diseno y organizacion.

## Orden recomendado de lectura

1. [ProjectOverview.md](ProjectOverview.md)
2. [ProjectStructure.md](ProjectStructure.md)
3. [RuntimeHierarchyAudit.md](RuntimeHierarchyAudit.md)
4. [WorldAndCamera.md](WorldAndCamera.md)
5. [WorldScale.md](WorldScale.md)
6. [StateMachines.md](StateMachines.md)
7. [CoreSystems.md](CoreSystems.md)
8. [GameplaySystems.md](GameplaySystems.md)
9. [EnemiesAndBosses.md](EnemiesAndBosses.md)
10. [Portals.md](Portals.md)
11. [ZoneLighting.md](ZoneLighting.md)
12. [UiAndMenu.md](UiAndMenu.md)
13. [QATester.md](QATester.md)
14. [ROADMAP.md](ROADMAP.md)

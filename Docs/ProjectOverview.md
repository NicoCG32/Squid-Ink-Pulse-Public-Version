# Resumen del proyecto

## Que es

Squid Ink-Pulse es un endless runner 2D en Unity desarrollado por nuestro equipo Yeco Works. Como equipo construimos una experiencia de acción submarina donde el jugador controla a un calamar bebé que avanza de forma continua, evita amenazas, recolecta camarones y usa Ink-Pulse como herramienta central de riesgo y recompensa.

La idea principal no es esquivar todo el peligro, sino aprender a acercarse al peligro de forma controlada. Cuando el jugador pasa cerca de amenazas sin colisionar, carga Ink-Pulse; al activarlo, obtiene una ventaja breve para sostener la run, atravesar situaciones críticas y aumentar su rendimiento.

## Pilares

- Riesgo y recompensa: acercarse a amenazas carga el recurso principal.
- Ritmo constante: el juego solo se detiene por estados globales o overlays temporales controlados.
- Claridad visual: el entorno debe leerse rápido incluso cuando la densidad sube.
- Rejugabilidad: progresión de run, bosses, tienda, gadgets, mejoras y portales modifican cada intento.
- Persistencia local: el progreso del jugador se guarda por dispositivo sin depender de servicios externos.

## Estado final de entrega

Como equipo desarrollamos e integramos:

- Movimiento del jugador limitado por `PlayerBoundaries`.
- Jugador canónico como `Assets/Content/Prefabs/Player/BabySquid.prefab`.
- Cámara limitada por `CameraBoundaries`.
- Spawn por perfiles de enemigo, con tags centralizados.
- SS Carnage integrado con progresión y red ajustada a boundaries.
- Tienda temporal mediante `DealerFish` e `InGameShopManager`.
- Gadgets de run comprables y no stackeables.
- Desbloqueos permanentes de elegibilidad de gadgets por hitos de score/records.
- Tienda out-of-game `ShopMenu` con mejoras permanentes, skins, compra, equipado y paginación por JSON.
- Persistencia JSON local para perfil, records, catalogo y leaderboard local.
- Portales entre `ZonaEpipelagica` y `ZonaAbisopelagica`.
- Comic tutorial accesible desde `MainMenu` mediante el botón `Cómo Jugar`.
- Comics de lore para inicio, portales, tienda y derrota.
- Gadgets e Ink-Pulse persistentes entre portales y reiniciados al entrar en Game Over.
- `ZonaAbisopelagica` con oscuridad ambiental, `LightGraze` visual y boss abisal.
- Soundtrack dinámico: crossfade entre pista normal e `INK` durante Ink-Pulse y progresión de pitch por avance de run.
- Volumen maestro global que afecta soundtrack normal, soundtrack Ink-Pulse, botones, inkbar y SFX.

## Add-on de feria

También implementamos un add-on opcional para presentaciones presenciales. Este add-on permite levantar `Tools/FairServer/` en un PC host, guardar una base SQLite y mostrar un leaderboard web en la red local.

Su alcance final es limitado y debe comunicarse asi:

- El host almacena el leaderboard de feria.
- Los resultados guardados formalmente son los jugados desde el PC host.
- Otros dispositivos solo visualizan el leaderboard web del host desde navegador.
- Cada build genera `README_SERVIDOR_FERIA.txt` y scripts de reinicio local.
- Si no hay host activo, pueden aparecer warnings rojos que se ignoran durante pruebas locales del juego.
- No cerramos la sincronizacion completa de progreso, compras, skins, mejoras o recuperacion integral entre PCs.

Por esta razon, la feria se documenta como add-on de leaderboard host y no como sistema remoto completo de guardado.

## Estructura general

- `Assets/Implementation/Code/`: lógica de juego, UI, boss, cámara, spawn, feria opcional y utilidades.
- `Assets/Content/`: arte, audio, prefabs y animaciones runtime.
- `Assets/Scenes/`: escenas del juego.
- `Assets/StreamingAssets/db/`: semillas JSON de persistencia local.
- `Docs/`: documentación técnica, diseño, cierre de entrega y guía de feria.
- `Tools/FairServer/`: servidor opcional para leaderboard de feria.

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
11. [LoreComics.md](LoreComics.md)
12. [ZoneLighting.md](ZoneLighting.md)
13. [UiAndMenu.md](UiAndMenu.md)
14. [FairServer.md](FairServer.md)
15. [FairEventSetupGuide.md](FairEventSetupGuide.md)
16. [ROADMAP.md](ROADMAP.md)

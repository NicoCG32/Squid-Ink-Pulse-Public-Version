# Informe breve del proyecto

## Resumen

Squid Ink-Pulse es un endless runner 2D centrado en riesgo y recompensa. El jugador controla a un calamar bebe que avanza por un entorno submarino hostil y obtiene ventaja al acercarse al peligro para cargar Ink-Pulse.

## Nucleo jugable

- Movimiento continuo con toma de decisiones rapida.
- Ink-Pulse como recurso cargado por proximidad al peligro.
- Progresion de run que aumenta velocidad, densidad y eventos.
- Boss SS Carnage como prueba de lectura, reaccion y uso del recurso.
- Economia persistente de camarones para comprar gadgets durante la run y mejoras fuera de la run.

## Implementacion viva

La implementacion actual ya incluye:
- boundaries formales por `PlayerBoundaries` y `CameraBoundaries`;
- enemigos por perfiles y tags centralizados;
- tienda temporal mediante `DealerFish`;
- gadgets `Shell Shield` e `Ink-Bottle`;
- portales entre `ZonaEpipelagica` y `ZonaAbisopelagica`;
- persistencia runtime de gadgets e Ink-Pulse entre portales;
- base JSON local para catalogo de desbloqueables, perfil, records y leaderboard;
- desbloqueo permanente de elegibilidad de gadgets por hitos;
- servicio transaccional para futura tienda out-of-game de skins y mejoras permanentes.

## Lectura complementaria

Este archivo es un resumen breve. El informe extenso de MVP se mantiene con caracter historico en `Docs/Reports/InformeSquidInkPulse.md`. Para decisiones actuales de implementacion, usar los documentos vivos de `Docs/`.

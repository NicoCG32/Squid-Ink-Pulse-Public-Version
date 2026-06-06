# Tutorial

## Alcance

`ZonaTutorial` es una escena futura para ensenar mecanicas de forma gradual antes de entrar al flujo completo de run. Ya esta registrada como escena conocida por `SceneFlowController`, pero todavia no debe considerarse una zona de gameplay normal.

## Secuencia prevista

El tutorial debe presentar las mecanicas en este orden:

1. Movimiento vertical dentro de `PlayerBoundaries`.
2. Graze como riesgo controlado para cargar Ink-Pulse.
3. Activacion de Ink-Pulse.
4. Aparicion de tienda temporal.
5. Compra y uso de gadgets.
6. Evento de boss con SS Carnage y pared/red.
7. Portal hacia zona de juego.

## Controlador futuro

La implementacion recomendada es un `TutorialDirector` dedicado.

Responsabilidades:
- Activar pasos en orden.
- Bloquear o habilitar mecanicas segun el paso activo.
- Solicitar spawns tutorializados sin usar la progresion normal como fuente primaria.
- Medir criterios de avance, por ejemplo moverse, cargar Ink-Pulse, comprar un gadget o cruzar un portal.
- Terminar cargando `ZonaEpipelagica` mediante `SceneFlowController`.

Reglas:
- No meter excepciones de tutorial en `LevelSpawner` si el comportamiento solo existe para ensenar.
- No alterar `RunProgressionDirector` para pasos pedagogicos.
- Mantener `PlayerBoundaries` y `CameraBoundaries` como contrato obligatorio tambien en tutorial.
- Si un paso necesita texto, marcador o overlay, debe pertenecer a UI de tutorial, no a los prefabs de enemigos.

## Menus relacionados

`ShopMenu` y `OptionsMenu` ya existen como escenas preparadas para trabajo posterior:
- `ShopMenu`: tienda global de skills, skins y bonificaciones.
- `OptionsMenu`: volumen, pantalla y dificultad.

Estos menus no reemplazan la tienda temporal in-game. Son flujos de menu, no eventos de mundo.

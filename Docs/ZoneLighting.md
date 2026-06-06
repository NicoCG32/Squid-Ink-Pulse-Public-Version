# Iluminacion de ZonaExe

## Proposito

`ZonaExe` se diferencia de la zona principal mediante oscuridad ambiental local. La implementacion no duplica fondos: el fondo claro sigue siendo el fondo real de escena y una capa negra semitransparente, `LayerBlack`, lo cubre. Las entidades con `LightGrazeSource` crean una mascara circular que rompe esa capa negra en un radio pequeno alrededor de la entidad, mas una pluma radial que suaviza el borde de la perforacion.

## Scripts

| Script | Ubicacion | Responsabilidad |
| --- | --- | --- |
| `ZoneLightingController` | `Assets/Implementation/Code/World/Lighting/ZoneLightingController.cs` | Controla `LayerBlack`, su opacidad y el sprite circular usado por las mascaras. |
| `LightGrazeSource` | `Assets/Implementation/Code/World/Lighting/LightGrazeSource.cs` | Crea una mascara circular local que perfora `LayerBlack` y un borde radial suave. |

## Contrato de escena

`ZonaExe` debe tener:

- `Enviroment/ZoneLightingController`
- `Enviroment/ZoneLightingController/LayerBlack`
- `ZoneLightingController.layerBlack` apuntando al `SpriteRenderer` de `LayerBlack`
- `ZoneLightingController.targetCamera` apuntando a `Main Camera`

`LayerBlack` debe renderizar sobre el fondo y bajo entidades de gameplay. La escena actual lo deja en sorting order `-1`, con fondos por debajo y jugador/enemigos por encima. Su `SpriteRenderer.maskInteraction` debe estar en `VisibleOutsideMask`, de modo que la capa negra se dibuje fuera de las mascaras y quede perforada donde exista una mascara circular.

## Contrato de entidades

Las entidades de mundo relevantes en `ZonaExe` reciben `LightGrazeSource`:

- BabySquid lo tiene en escena.
- `LevelSpawner` lo agrega a camarones, enemigos, `DealerFish` y portales si existe `ZoneLightingController` activo.

Los prefabs compartidos no deben depender de esta mecanica. `SSCarnage` y `BossNetWall` no participan en este sistema porque no aparecen en `ZonaExe`.

`LightGrazeSource` crea en runtime un hijo `LightGrazeMask` con `SpriteMask` y, si `lightEdgeSoftness` es mayor que cero, un hijo `LightGrazeFeather` con `SpriteRenderer`. La pluma usa `VisibleInsideMask`, por lo que solo se dibuja dentro del agujero de luz y no agrega un contorno negro sobre `LayerBlack`. El radio y la suavidad se definen en `ZoneLightingController`, no en la entidad.

## Diferencia con GrazeDetector

`GrazeDetector` sigue siendo el sistema puntuable/mecanico del Ink-Pulse:

- detecta amenazas;
- carga Ink-Pulse;
- pertenece al loop de riesgo-recompensa.

`LightGrazeSource` es solo visual:

- no carga Ink-Pulse;
- no depende del `GrazeZone`;
- no requiere triggers de graze;
- no mide distancia al jugador;
- perfora `LayerBlack` localmente alrededor de la entidad que lo posee.

Esta separacion permite ajustar lectura visual de `ZonaExe` sin alterar economia, carga de Ink-Pulse ni dificultad directa.

## Parametros ajustables

Owner: `ZoneLightingController`.

| Campo | Que controla |
| --- | --- |
| `blackAlpha` | Opacidad fija de `LayerBlack`. |
| `overlayPadding` | Margen extra para cubrir toda la camara aunque cambie aspect ratio. |
| `maskSortingOrderPadding` | Rango de sorting usado por las mascaras circulares. |
| `lightHoleRadius` | Radio de mundo de cada perforacion circular. |
| `lightEdgeSoftness` | Proporcion del radio usada como borde suave. `0` equivale a borde duro. |
| `maskAlphaCutoff` | Umbral alfa del sprite circular usado como mascara. |

## Regla de mantenimiento

No agregar parametros de luz a enemigos, camarones, portales o tienda. Las entidades solo pueden recibir `LightGrazeSource`; el balance visual pertenece al `ZoneLightingController`.

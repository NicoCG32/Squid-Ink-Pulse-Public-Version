# Iluminacion de ZonaExe

## Proposito

`ZonaExe` se diferencia de la zona principal mediante oscuridad ambiental y revelado temporal por proximidad. La implementacion no duplica fondos: el fondo claro sigue siendo el fondo real de escena y un overlay oscuro lo cubre. Cuando el jugador pasa cerca de una fuente de `LightGrazeSource`, el overlay reduce su opacidad y el entorno vuelve momentaneamente a su iluminacion normal.

## Scripts

| Script | Ubicacion | Responsabilidad |
| --- | --- | --- |
| `ZoneLightingController` | `Assets/Implementation/Code/World/Lighting/ZoneLightingController.cs` | Controla opacidad, duracion y transicion del overlay oscuro de la zona. |
| `LightGrazeSource` | `Assets/Implementation/Code/World/Lighting/LightGrazeSource.cs` | Marca una entidad como fuente que puede revelar luz al pasar cerca. |
| `LightGrazeProbe` | `Assets/Implementation/Code/World/Lighting/LightGrazeProbe.cs` | Vive en el BabySquid y detecta fuentes cercanas mientras la partida esta activa. |

## Contrato de escena

`ZonaExe` debe tener:

- `Enviroment/ZoneLightingController`
- `Enviroment/ZoneLightingController/DarknessOverlay`
- `ZoneLightingController.darknessOverlay` apuntando al `SpriteRenderer` de `DarknessOverlay`
- `ZoneLightingController.targetCamera` apuntando a `Main Camera`

`DarknessOverlay` debe renderizar sobre el fondo y bajo entidades de gameplay. La escena actual lo deja en sorting order `-1`, con fondos por debajo y jugador/enemigos por encima.

## Contrato de entidades

Las entidades de mundo relevantes deben tener `LightGrazeSource`:

- `ShrimpCoin`
- `ShrimpCoinX10`
- `PezGlobo`
- `Mina`
- `CanaPescar`
- `DealerFish`
- `ScenePortal`
- `SSCarnage`
- `BossNetWall`

Ademas, `LevelSpawner`, `BossEventDirector` y `SSCarnageController` llaman `LightGrazeSource.EnsureOn()` sobre objetos instanciados. Esto mantiene el contrato aunque un prefab sea reemplazado durante produccion.

El BabySquid tiene `LightGrazeSource` y `LightGrazeProbe`. La sonda ignora fuentes que compartan la misma raiz, por lo que la fuente propia del jugador no mantiene el mundo iluminado permanentemente.

## Diferencia con GrazeDetector

`GrazeDetector` sigue siendo el sistema puntuable/mecanico del Ink-Pulse:

- detecta amenazas;
- carga Ink-Pulse;
- pertenece al loop de riesgo-recompensa.

`LightGrazeProbe` es solo visual:

- no carga Ink-Pulse;
- no depende del `GrazeZone`;
- no requiere triggers de graze;
- mide distancia contra el punto mas cercano de colliders habilitados o bounds visuales.

Esta separacion permite ajustar lectura visual de `ZonaExe` sin alterar economia, carga de Ink-Pulse ni dificultad directa.

## Parametros ajustables

Owner: `ZoneLightingController`.

| Campo | Que controla |
| --- | --- |
| `darkAlpha` | Opacidad normal del overlay oscuro. |
| `litAlpha` | Opacidad objetivo cuando ocurre light graze. |
| `litHoldSeconds` | Tiempo que la luz se mantiene tras detectar una fuente. |
| `fadeToLitSpeed` | Velocidad con que el fondo se aclara. |
| `fadeToDarkSpeed` | Velocidad con que vuelve la oscuridad. |
| `overlayPadding` | Margen extra para cubrir toda la camara aunque cambie aspect ratio. |
| `lightGrazeRadius` | Distancia desde BabySquid a la entidad para revelar luz. |

## Regla de mantenimiento

No agregar parametros de luz a enemigos, camarones, portales o tienda. Las entidades solo declaran `LightGrazeSource`; el balance visual pertenece al `ZoneLightingController`.

# Iluminacion de ZonaAbisopelagica

## Proposito

`ZonaAbisopelagica` se diferencia de la zona principal mediante oscuridad ambiental local. La implementación no duplica fondos: el fondo claro sigue siendo el fondo real de escena y una capa negra semitransparente, `LayerBlack`, lo cubre. Las entidades con `LightGrazeSource` declaran centros de luz y `ZoneLightingController` genera un overlay compuesto que revela esa oscuridad con bordes suaves.

## Scripts

| Script | Ubicacion | Responsabilidad |
| --- | --- | --- |
| `ZoneLightingController` | `Assets/Implementation/Code/World/Lighting/ZoneLightingController.cs` | Controla `LayerBlack`, su opacidad y el overlay compuesto de luz. |
| `LightGrazeSource` | `Assets/Implementation/Code/World/Lighting/LightGrazeSource.cs` | Registra la muestra visual de luz de una entidad: posición, forma local y titileo opcional. |

## Contrato de escena

`ZonaAbisopelagica` debe tener:

- `EnviromentRoot_ZonaAbisopelagica/ZoneLightingController`
- `EnviromentRoot_ZonaAbisopelagica/ZoneLightingController/LayerBlack`
- `ZoneLightingController.layerBlack` apuntando al `SpriteRenderer` de `LayerBlack`
- `ZoneLightingController.targetCamera` apuntando a `Main Camera`

`LayerBlack` debe renderizar sobre el fondo y bajo entidades de gameplay. La escena actual lo deja en sorting order `-1`, con fondos por debajo y jugador/enemigos por encima. En el modo actual, `useCompositeLightOverlay` esta activo: `ZoneLightingController` reemplaza el sprite runtime de `LayerBlack` por una textura generada y deja `maskInteraction` en `None`. Si se desactiva ese modo, el sistema vuelve al modo legacy con `SpriteMask` y `VisibleOutsideMask`.

## Contrato de entidades

Las entidades de mundo relevantes en `ZonaAbisopelagica` reciben `LightGrazeSource`:

- La instancia `Squid` de `BabySquid.prefab` lo tiene como override de escena en `ZonaAbisopelagica`.
- `SpawnedObjectConfigurator`, invocado por `LevelSpawner`, lo agrega a camarones, enemigos, `DealerFish` y portales si existe `ZoneLightingController` activo.

Los prefabs compartidos no deben depender de esta mecánica; el prefab base `BabySquid` tampoco debe incluirla. `SSCarnage` y `BossNetWall` no participan en este sistema porque no aparecen en `ZonaAbisopelagica`.

En modo compuesto, `LightGrazeSource` no crea renderers visibles por entidad: solo participa en una lista runtime de posiciones. `ZoneLightingController` calcula una unica textura de oscuridad y, cuando dos luces se cruzan, toma la menor opacidad por pixel. Esto evita que dos halos se sumen y generen manchas negras o sobreposicion artificial. El radio, la suavidad y la resolucion del overlay se definen en `ZoneLightingController`, no en la entidad.

`LightGrazeSource` puede especializar la lectura visual de una entidad sin cambiar gameplay:
- `grazeAnchor` permite mover el centro de la luz a un hijo concreto del prefab.
- Si `grazeAnchor` no esta asignado, el componente busca en este orden: `GrazeLightAnchor`, `VisualSupport`, `Roca`, `Rock`, `Visual`; si no encuentra ninguno, usa el root.
- `lightShapeScale` permite luces elipticas. El radio base sigue viniendo de `ZoneLightingController`, pero cada entidad puede escalar X/Y.
- `flickerEnabled` alterna la emision visual de la fuente usando rangos de encendido/apagado en tiempo de juego.

Casos actuales:
- `DealerFish_ZonaAbisopelagica` debe iluminar desde su soporte visual/roca, no desde el fondo del prefab.
- `UnknownBoss` / boss abisal usa luz propia adaptada a su forma y titilante, porque su lectura visual corresponde a una anguila electrica.

## Rendimiento del modo compuesto

El overlay compuesto esta optimizado para no escalar con objetos fuera de cámara. El flujo esperado es:

1. Calcular el area visible de cámara con margen.
2. Recolectar solo `LightGrazeSource` activos dentro de esa area ampliada por el radio de luz.
3. Rellenar la textura con la opacidad negra base.
4. Pintar solo el rectangulo de pixeles que puede afectar cada luz visible.
5. Aplicar la textura a una frecuencia configurable mediante `compositeUpdatesPerSecond`.

No debe volver al algoritmo de recorrer toda la textura y comparar cada pixel contra todas las fuentes activas. Ese enfoque cuesta `ancho * alto * fuentes` por actualizacion y degrada especialmente en `ZonaAbisopelagica`, donde camarones, enemigos, `DealerFish`, portales y jugador pueden declarar `LightGrazeSource`.

La reduccion de frecuencia es intencional: la iluminacion es feedback ambiental, no una mecánica de precision. El contrato actual usa `60` recomposiciones por segundo para que el halo no se perciba entrecortado durante Ink-Pulse. Si el costo vuelve a ser alto, bajarlo o reducir `compositeTextureWidth`/`compositeTextureHeight`.

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
- revela `LayerBlack` localmente alrededor de la entidad que lo posee.

Esta separacion permite ajustar lectura visual de `ZonaAbisopelagica` sin alterar economia, carga de Ink-Pulse ni dificultad directa.

## Parámetros ajustables

Owner: `ZoneLightingController`.

| Campo | Que controla |
| --- | --- |
| `blackAlpha` | Opacidad fija de `LayerBlack`. |
| `overlayPadding` | Margen extra para cubrir toda la cámara aunque cambie aspect ratio. |
| `maskSortingOrderPadding` | Rango de sorting usado solo por el modo fallback con `SpriteMask`. |
| `lightHoleRadius` | Radio de mundo de cada zona revelada. |
| `lightEdgeSoftness` | Proporcion del radio usada como borde suave. `0` equivale a borde duro. |
| `maskAlphaCutoff` | Umbral alfa del sprite circular usado solo por el modo fallback con `SpriteMask`. |
| `useCompositeLightOverlay` | Activa el overlay compuesto que evita acumulacion visual entre luces. |
| `compositeTextureWidth` | Resolucion horizontal de la textura runtime de oscuridad. |
| `compositeTextureHeight` | Resolucion vertical de la textura runtime de oscuridad. |
| `compositeUpdatesPerSecond` | Frecuencia maxima a la que se recompone la textura. |
| `lightSourceCullingPadding` | Margen extra, en unidades de mundo, para considerar fuentes cercanas al borde de cámara. |

Owner: `LightGrazeSource`.

| Campo | Que controla |
| --- | --- |
| `grazeAnchor` | Transform usado como centro de la luz. |
| `lightShapeScale` | Escala relativa X/Y del radio base, util para formas elipticas. |
| `flickerEnabled` | Activa titileo de la fuente. |
| `flickerOnDurationRange` | Rango de duracion de encendido. |
| `flickerOffDurationRange` | Rango de duracion de apagado. |
| `randomizeInitialFlicker` | Evita que todas las luces titilantes arranquen sincronizadas. |

## Regla de mantenimiento

No agregar parámetros globales de balance de luz a enemigos, camarones, portales o tienda. Las entidades pueden declarar `LightGrazeSource` con ancla, forma y titileo local; el radio base, opacidad, suavidad, resolucion y frecuencia de recomposicion pertenecen al `ZoneLightingController`.

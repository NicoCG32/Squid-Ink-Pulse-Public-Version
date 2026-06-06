# Escala estandar de mundo

## Proposito

Este documento fija la relacion canonica entre pixeles de arte, unidades de Unity, boundaries y prefabs. Su objetivo es que escenario, enemigos, red del SS Carnage y futuros gadgets se ajusten con una misma regla medible, sin depender de escalas visuales arbitrarias.

## Convencion canonica

- `100 px` de arte equivalen a `1 unidad Unity`.
- `1 px` de arte equivale a `0.01 unidades Unity`.
- `10 px` de arte equivalen a `0.1 unidades Unity`.
- Los sprites 2D nuevos deben importarse con `Pixels Per Unit = 100`, salvo excepcion artistica documentada.

Formula general:

```text
unidades_de_mundo = pixeles_del_sprite / pixels_per_unit
pixeles_equivalentes = unidades_de_mundo * pixels_per_unit
```

Ejemplo: un sprite de `1469 px` de alto mide `14.69 unidades Unity` cuando se importa a `100 PPU` y se usa con escala local `1`.

Importante: si el `SpriteRenderer` esta en `Draw Mode = Simple`, el campo `Size` del renderer no define el tamano visible. La medida visible sale del rect del sprite importado, su `Pixels Per Unit` y la escala efectiva del `Transform` en mundo. Para conservar relacion `1:1`, la escala relevante es `transform.lossyScale`, no solo `transform.localScale`.

## Boundaries del jugador

La ventana vertical util del jugador se define por la jerarquia obligatoria:

```text
PlayerBoundaries
├── TopBoundary
└── BottomBoundary
```

La altura util no se mide desde la posicion central de los transforms, sino desde sus bordes fisicos internos:

- borde superior de `BottomBoundary`: `bottomCollider.bounds.max.y`;
- borde inferior de `TopBoundary`: `topCollider.bounds.min.y`.

En la configuracion actual de `ZonaEpipelagica`, esa distancia esta normalizada en `14.69 unidades Unity`, equivalentes a `1469 px` de arte a `100 PPU`.

## Boundaries de camara

La camara usa una jerarquia separada:

```text
CameraBoundaries
├── TopBoundary
└── BottomBoundary
```

Estos colliders definen hasta donde puede desplazarse el encuadre. No deben mezclarse con `PlayerBoundaries`, porque el jugador y la camara tienen restricciones distintas.

## Red del SS Carnage

El prefab `BossNetWall` usa la misma fuente de verdad que el jugador:

- origen vertical runtime: borde superior de `BottomBoundary`;
- destino vertical runtime: borde inferior de `TopBoundary`;
- altura runtime: distancia fisica entre ambos bordes;
- referencia visual de autor: el prefab conserva sus proporciones y se escala de forma uniforme.

El objeto hijo `WallReferenceRectangle` es la referencia fisica de la pared. Debe quedar dimensionado como la altura util entre `PlayerBoundaries`. El collider puede reemplazarse o ajustarse luego al contorno real de la red, pero la fuente de altura no cambia.

El prefab tambien puede conservar un hijo inactivo `AuthoringPlayerBoundaries`. Este objeto no es un boundary runtime; sirve solo como regla de autor dentro del prefab. `SSCarnageNetWall` lo usa para conocer que tramo local del prefab debe mapearse contra los `PlayerBoundaries` reales de la escena. Por eso:

- no debe llamarse `PlayerBoundaries`;
- debe permanecer inactivo en runtime;
- sus hijos pueden llamarse `TopBoundary` y `BottomBoundary` porque estan encapsulados bajo `AuthoringPlayerBoundaries`;
- si se mueven los sprites de la red, conviene ajustar esta referencia authored para que el tramo inferior/superior siga representando la altura jugable.

En runtime, `SSCarnageNetWall` calcula la escala de la red contra `PlayerBoundaries` en espacio de mundo y compensa la escala de cualquier padre de jerarquia. Esto evita que un prefab que cuadra en edicion se vea mas pequeno o mas grande al entrar en Play.

## Reglas practicas

- Ajustar sprites desde importacion y escala local antes de compensar con codigo.
- Mantener prefabs visuales en escala local `1` siempre que sea posible.
- Usar `PlayerBoundaries` y `CameraBoundaries` como unica fuente de limites de gameplay.
- No deformar sprites estirando un solo eje, salvo elementos geometricos sin lectura artistica.
- No introducir rangos manuales de respaldo para corregir una escena mal configurada.
- Documentar cualquier excepcion a `100 PPU` en este archivo o en la ficha del sistema correspondiente.

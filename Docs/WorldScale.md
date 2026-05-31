# Escala estandar de mundo

## Proposito

Este documento fija la relacion canonica entre pixeles de arte, unidades de Unity, boundaries y prefabs. Su objetivo es que el escenario, los enemigos, la red del SS Carnage y los futuros gadgets se ajusten con una misma regla medible, sin depender de escalas visuales arbitrarias.

## Convencion canonica

- `100 px` de arte equivalen a `1 unidad Unity`.
- `1 px` de arte equivale a `0.01 unidades Unity`.
- `10 px` de arte equivalen a `0.1 unidades Unity`.
- Los sprites 2D nuevos deben importarse con `Pixels Per Unit = 100`, salvo que exista una razon artistica documentada para romper esta regla.

Esta convencion permite hablar de dimensiones del mundo en pixeles sin perder precision tecnica. Por ejemplo, un sprite de `1469 px` de alto mide `14.69 unidades Unity` cuando se importa a `100 PPU` y se usa con escala local `1`.

Formula general:

```text
unidades_de_mundo = pixeles_del_sprite / pixels_per_unit
pixeles_equivalentes = unidades_de_mundo * pixels_per_unit
```

## Boundaries del jugador

En `ZonaEpipelagica`, la ventana vertical util del jugador queda estandarizada en `14.69 unidades Unity`, equivalentes a `1469 px` de arte.

La altura util no se mide desde la posicion central de los objetos `TopBoundary` y `BottomBoundary`, sino desde sus bordes fisicos internos:

- borde superior del `BottomBoundary`;
- borde inferior del `TopBoundary`.

Esta distincion es importante porque los boundaries tienen `BoxCollider2D`, escala y offset. Los sistemas runtime deben leer los bounds fisicos, no solo la posicion del transform.

## Red del SS Carnage

El prefab `BossNetWall` usa la misma altura canonica:

- altura de autor en el prefab: `14.69 unidades Unity`;
- equivalencia visual: `1469 px` a `100 PPU`;
- origen vertical del prefab: borde inferior de la red;
- destino runtime: distancia fisica entre `BottomBoundary` y `TopBoundary` del jugador.

La red conserva la proporcion de sus sprites. Si la altura entre boundaries cambia, el sistema escala el grupo visual de forma uniforme. El collider se ajusta de manera independiente para cubrir exactamente la altura jugable.

Para que esta relacion sea estable en el editor, las capas visuales de la red deben conservar pivote inferior en sus sprites. Asi, `localPosition.y = 0` representa el borde inferior visual y `localPosition.y + altura_del_sprite` representa el borde superior.

El prefab mantiene un objeto hijo llamado `WallReferenceRectangle`. Este rectangulo rojo es la referencia fisica de la pared:

- posicion local: centro vertical en `7.345`;
- ancho: `0.75` unidades;
- alto: `14.69` unidades;
- collider: `PolygonCollider2D` rectangular, editable luego al contorno real de la red.

Con esta estructura, la red artistica puede moverse y escalarse contra una referencia estable, mientras la pared de gameplay conserva una altura equivalente a la ventana vertical util del jugador.

## Reglas practicas

- Ajustar sprites desde su importacion y escala local antes de compensar con codigo.
- Mantener prefabs visuales en escala local `1` siempre que sea posible.
- Usar los boundaries como fuente de verdad para limites de gameplay.
- No deformar sprites estirando solo un eje, salvo en elementos geometricos sin lectura artistica.
- Documentar cualquier excepcion a `100 PPU` en este archivo o en la ficha del sistema correspondiente.

# Escala estándar de mundo

## Propósito

Este documento fija la relación canónica entre píxeles de arte, unidades de Unity, boundaries y prefabs. Su objetivo es que el escenario, los enemigos, la red del SS Carnage y los futuros gadgets se ajusten con una misma regla medible, sin depender de escalas visuales arbitrarias.

## Convención canónica

- `100 px` de arte equivalen a `1 unidad Unity`.
- `1 px` de arte equivale a `0.01 unidades Unity`.
- `10 px` de arte equivalen a `0.1 unidades Unity`.
- Los sprites 2D nuevos deben importarse con `Pixels Per Unit = 100`, salvo que exista una razón artística documentada para romper esta regla.

Esta convención permite hablar de dimensiones del mundo en píxeles sin perder precisión técnica. Por ejemplo, un sprite de `1469 px` de alto mide `14.69 unidades Unity` cuando se importa a `100 PPU` y se usa con escala local `1`.

Formula general:

```text
unidades_de_mundo = pixeles_del_sprite / pixels_per_unit
pixeles_equivalentes = unidades_de_mundo * pixels_per_unit
```

## Boundaries del jugador

En `ZonaEpipelágica`, la ventana vertical útil del jugador queda estandarizada en `14.69 unidades Unity`, equivalentes a `1469 px` de arte.

La altura útil no se mide desde la posición central de los objetos `TopBoundary` y `BottomBoundary`, sino desde sus bordes físicos internos:

- borde superior del `BottomBoundary`;
- borde inferior del `TopBoundary`.

Esta distinción es importante porque los boundaries tienen `BoxCollider2D`, escala y offset. Los sistemas runtime deben leer los bounds físicos, no solo la posición del transform.

## Red del SS Carnage

El prefab `BossNetWall` usa la misma altura canónica:

- altura de autor en el prefab: `14.69 unidades Unity`;
- equivalencia visual: `1469 px` a `100 PPU`;
- origen vertical del prefab: borde inferior de la red;
- destino runtime: distancia física entre `BottomBoundary` y `TopBoundary` del jugador.

La red conserva la proporción de sus sprites. Si la altura entre boundaries cambia, el sistema escala el grupo visual de forma uniforme. El collider se ajusta de manera independiente para cubrir exactamente la altura jugable.

Para que esta relación sea estable en el editor, las capas visuales de la red deben conservar pivote inferior en sus sprites. Así, `localPosition.y = 0` representa el borde inferior visual y `localPosition.y + altura_del_sprite` representa el borde superior.

El prefab mantiene un objeto hijo llamado `WallReferenceRectangle`. Este rectángulo rojo es la referencia física de la pared:

- posición local: centro vertical en `7.345`;
- ancho: `0.75` unidades;
- alto: `14.69` unidades;
- collider: `PolygonCollider2D` rectangular, editable luego al contorno real de la red.

Con esta estructura, la red artística puede moverse y escalarse contra una referencia estable, mientras la pared de gameplay conserva una altura equivalente a la ventana vertical útil del jugador.

## Reglas practicas

- Ajustar sprites desde su importación y escala local antes de compensar con código.
- Mantener prefabs visuales en escala local `1` siempre que sea posible.
- Usar los boundaries como fuente de verdad para límites de gameplay.
- No deformar sprites estirando solo un eje, salvo en elementos geométricos sin lectura artística.
- Documentar cualquier excepción a `100 PPU` en este archivo o en la ficha del sistema correspondiente.

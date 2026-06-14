# Mundo y camara

## Alcance

Este documento cubre camara, boundaries, destruccion fuera de pantalla, seguimiento horizontal, parallax y overlays de zona.

## CameraController

Archivo: `Assets/Implementation/Code/Core/Camera/CameraController.cs`

Responsabilidad:
- Seguir al jugador con una camara suave.
- Entrar en vista amplia para eventos.
- Volver de forma interpolada al seguimiento normal.
- Aplicar un feedback breve de tambaleo y pulso de zoom cuando `InkPulseController` emite `PulseStarted`.
- Limitar su posicion vertical usando exclusivamente `CameraBoundaries`.

`Main Camera` vive bajo una instancia prefab `CameraRig_*` por zona en `Assets/Content/Prefabs/Core/Camera/`. El prefab estabiliza la composicion de camara, pero las referencias externas siguen siendo contrato de escena: `CameraController` puede resolver el jugador por tag `Player`, `Camera.main` y `CameraBoundaries` en runtime.

Estados de camara:
- `Follow`
- `WideEvent`
- `ReturningToFollow`

Feedback de Ink-Pulse:
- Vive en `CameraController`, no en `InkPulseController`, porque es una consecuencia visual de camara.
- La referencia `inkPulse` puede resolverse desde el `target`.
- Los parametros `inkPulseFeedbackDuration`, `inkPulseShakeAmplitude`, `inkPulseZoomAmplitude` e `inkPulseShakeFrequency` son ajustables para QA.

## BoundaryReferenceResolver

Archivo: `Assets/Implementation/Code/Core/World/BoundaryReferenceResolver.cs`

Responsabilidad:
- Resolver las fronteras de camara y jugador sin duplicar referencias en cada componente.
- Tratar la ausencia de jerarquia obligatoria como error de configuracion de escena.
- Entregar rangos verticales internos a sistemas de movimiento, spawn, camara y boss.

Contrato obligatorio:

| Dominio | Contenedor exacto | Hijos exactos |
| --- | --- | --- |
| Jugador | `PlayerBoundaries` | `TopBoundary`, `BottomBoundary` |
| Camara | `CameraBoundaries` | `TopBoundary`, `BottomBoundary` |

Estos contenedores viven bajo una instancia de `Assets/Content/Prefabs/World/Boundaries.prefab`. El prefab define la estructura y el `HorizontalTracker`; cada zona conserva overrides de colliders para su altura real.

Reglas:
- Los nombres son contrato de codigo, no convencion opcional.
- Los hijos deben tener `Collider2D`.
- El rango util se calcula desde `bottom.bounds.max.y` hasta `top.bounds.min.y`.
- No se usan tags para encontrar boundaries.
- No se asignan `topBorder` ni `bottomBorder` en Inspector.
- No existen rangos manuales de respaldo para gameplay normal.
- No se desempaqueta ni duplica la jerarquia para resolver un caso local.

Consumidores actuales:
- `PlayerMovement` limita al squid con `PlayerBoundaries`.
- `LevelSpawner` posiciona enemigos, `DealerFish` y portales desde `PlayerBoundaries`; los camarones usan la interseccion entre viewport, `CameraBoundaries` y `PlayerBoundaries`.
- `CameraController` limita la camara con `CameraBoundaries`.
- `BossEventDirector` valida `CameraBoundaries` antes del evento.
- `SSCarnageController` usa `PlayerBoundaries` para colocarse sobre el limite superior del jugador.
- `SSCarnageNetWall` ajusta altura visual y colision con `PlayerBoundaries`.

## Escala de mundo

La escala canonica esta documentada en [WorldScale.md](WorldScale.md): `100 px` de arte equivalen a `1 unidad Unity`.

En `ZonaEpipelagica`, la altura fisica util entre el borde superior del `BottomBoundary` del jugador y el borde inferior del `TopBoundary` del jugador queda normalizada en `14.69` unidades. Esta medida corresponde a `1469 px` a `100 PPU`, y es la referencia directa para la altura de la red del SS Carnage.

## DestroyOffscreen

Archivo: `Assets/Implementation/Code/Core/World/DestroyOffscreen.cs`

Responsabilidad:
- Eliminar objetos que salen de pantalla.
- Evitar acumulacion innecesaria de enemigos, props o proyectiles.
- Reconocer amenazas y collectibles mediante catalogos de tags, no strings locales editables.
- Seguir el borde izquierdo de la camara de gameplay con un trigger vertical.
- Ajustar automaticamente el alto del trigger segun la distancia interna entre `CameraBoundaries/BottomBoundary` y `CameraBoundaries/TopBoundary`.
- Resolver el objeto a destruir desde el collider o sus padres, para soportar prefabs con colliders hijos.
- Destruir solo cuando el borde derecho completo del objeto ya cruzo el plano de cleanup, no cuando su primer collider toca la franja.
- Limpiar tambien objetos de boss con tag `SSCarnage`, como el boss y `BossNetWall`, cuando quedan atras.

Contrato:
- `CleanUp` debe ser una instancia de `Assets/Content/Prefabs/World/CleanUp.prefab`.
- La posicion del `GarbageCollector` en editor no es fuente de verdad runtime.
- La referencia tecnica `targetCamera` debe apuntar a la camara principal de gameplay; si falta, usa `Camera.main`.
- El alto del trigger se calcula desde `bottom.bounds.max.y` hasta `top.bounds.min.y` del dominio `Camera`.
- El trigger queda detras del borde izquierdo visible, por lo que limpia solo objetos que ya quedaron fuera de camara.
- La franja de trigger es detectora, no criterio final de destruccion. `DestroyOffscreen` calcula bounds agregados de colliders y renderers activos; el objeto se destruye cuando `bounds.max.x` queda detras del plano central del collector. Esto evita limpieza temprana en prefabs anchos como `BossNetWall`.
- Todo objeto limpiable debe conservar al menos un `Collider2D` trigger activo mientras exista visualmente. Si se desactiva el collider, el cleanup no puede detectarlo.
- No expone parametros de balance. Si el ritmo de acumulacion cambia, se ajusta el spawn o la progresion, no el limpiador.

## HorizontalTracker

Archivo: `Assets/Implementation/Code/Core/World/HorizontalTracker.cs`

Responsabilidad:
- Seguir el avance horizontal del mundo o de un objeto de referencia.
- Mantener alineados los contenedores de boundaries cuando la escena avanza.
- Servir de apoyo para logica de distancia o scroll.
- Resolver `Camera.main` si la instancia prefab de `Boundaries` no tiene una referencia de escena serializada.

## ParallaxLayer

Archivo: `Assets/Implementation/Code/Background/ParallaxLayer.cs`

Responsabilidad:
- Aplicar desplazamiento de fondo para dar sensacion de profundidad.
- Acompanar el avance de la run sin competir con la lectura del gameplay.
- Mantener la logica visual del fondo separada de los limites de gameplay.

## ZoneLightingController

Archivo: `Assets/Implementation/Code/World/Lighting/ZoneLightingController.cs`

Responsabilidad:
- Mantener `LayerBlack` centrado y escalado contra la camara ortografica activa.
- Oscurecer `ZonaAbisopelagica` sin modificar los fondos ni duplicar sprites claros/oscuros.
- Generar una textura compuesta de oscuridad a partir de las posiciones declaradas por `LightGrazeSource`.

La mecanica completa esta documentada en [ZoneLighting.md](ZoneLighting.md).

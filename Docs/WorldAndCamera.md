# Mundo y cámara

## Alcance

Este documento cubre el sistema de cámara, fronteras de juego, destrucción fuera de pantalla, seguimiento y fondo parallax.

## CameraController

Archivo: `Assets/Implementation/Code/Core/Camera/CameraController.cs`

Responsabilidad:
- Seguir al jugador con una cámara suave.
- Entrar en vista amplia para eventos.
- Volver de forma interpolada al seguimiento normal.

Estados de cámara:
- `Follow`
- `WideEvent`
- `ReturningToFollow`

## BoundaryReferenceResolver

Archivo: `Assets/Implementation/Code/Core/World/BoundaryReferenceResolver.cs`

Responsabilidad:
- Resolver las fronteras de cámara y jugador sin duplicar referencias en cada componente.

## Escala de mundo

La escala canónica está documentada en [WorldScale.md](WorldScale.md): `100 px` de arte equivalen a `1 unidad Unity`.

En `ZonaEpipelágica`, la altura física útil entre el borde superior del `BottomBoundary` del jugador y el borde inferior del `TopBoundary` del jugador queda normalizada en `14.69` unidades. Esta medida corresponde a `1469 px` a `100 PPU`, y es la referencia directa para la altura de la red del SS Carnage.

## DestroyOffscreen

Archivo: `Assets/Implementation/Code/Core/World/DestroyOffscreen.cs`

Responsabilidad:
- Eliminar objetos que salen de pantalla.
- Evitar acumulación innecesaria de enemigos, props o proyectiles.

## HorizontalTracker

Archivo: `Assets/Implementation/Code/Core/World/HorizontalTracker.cs`

Responsabilidad:
- Seguir el avance horizontal del mundo o de un objeto de referencia.
- Servir de apoyo para lógica de distancia o scroll.

## ParallaxLayer

Archivo: `Assets/Implementation/Code/Background/ParallaxLayer.cs`

Responsabilidad:
- Aplicar desplazamiento de fondo para dar sensación de profundidad.
- Acompañar el avance de la run sin competir con la lectura del gameplay.

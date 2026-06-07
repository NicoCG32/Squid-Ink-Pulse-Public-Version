# Estandares de animacion

## Ubicacion de archivos

- `Assets/Content/Animations/Characters/`: clips y controladores de personajes.
- `Assets/Content/Animations/Enemies/`: clips y controladores de enemigos.
- `Assets/Content/Animations/Environment/`: animaciones de escenario y props.
- `Assets/Content/Animations/UI/`: animaciones de interfaz y feedback visual.
- `Assets/Content/Animations/UI/MainMenu/Character/`: animaciones de personaje del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Background/`: animaciones de fondo del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Buttons/`: animaciones de botones del menu principal.

## Tipos de assets esperados

- Animation Clip: `.anim`
- Animator Controller: `.controller`
- Animator Override Controller: `.overrideController` cuando exista variante.

## Convenciones de nombres

- Clip: `Entidad_Accion`.
- Controller: `Entidad_Controller`.
- Override: `Entidad_Variante_Override`.

## Integracion con prefabs

1. Seleccionar el prefab o elemento UI correspondiente.
2. Asignar el `Animator Controller` al componente `Animator`.
3. Verificar parametros y transiciones requeridas por la logica.
4. Validar comportamiento en escena runtime.

## BabySquid

Prefab canonico:
- `Assets/Content/Prefabs/Player/BabySquid.prefab`

Animator Controllers:
- Cuerpo: `Assets/Content/Animations/Characters/BabySquid/Movement/Squid.controller`
- Efecto Ink-Pulse: `Assets/Content/Animations/Characters/BabySquid/InkPulse/InkPulseVisual.controller`
- Efecto portal: `Assets/Content/Animations/Environment/Portal/PortalEffect/PortalEffect.controller`

Clips:
- `Movement/Movement.anim`: animacion base de movimiento.
- `InkPulse/InkPulse.anim`: impulso visual de tinta.
- `Environment/Portal/PortalEffect/PortalEffect.anim`: transicion visual previa al cambio de escena.

Reglas:
- El root de prefab `BabySquid` gobierna estado y gameplay; en escena su instancia puede llamarse `Squid`.
- El root no debe usar animaciones para modificar colliders, posicion de gameplay ni input.
- `SquidVisual` contiene el `SpriteRenderer` y `Animator` del cuerpo. Su controller reproduce `Movement.anim`.
- `InkPulseVisual` contiene el `SpriteRenderer` y `Animator` del efecto largo de tinta.
- `PortalVisual` contiene el `SpriteRenderer` y `Animator` de `PortalEffect`.
- `InkPulse.anim` no debe hacer loop.
- `PortalEffect.anim` no debe hacer loop.
- `PlayerVisualStateController` vive en el root de `BabySquid` y decide que visual se muestra.
- Prioridad visual: `PortalVisual` > `InkPulseVisual` > `SquidVisual`.
- `InkPulseVisual` permanece oculto fuera de `PlayerRuntimeState.InkPulse`.
- `PortalVisual` permanece oculto fuera de `PlayerRuntimeState.PortalTransition`.
- Mientras un visual de mayor prioridad esta visible, `PlayerVisualStateController` oculta los renderers de los otros visuales para evitar doble cuerpo.
- `InkPulseVisual` debe renderizar por encima de `SquidVisual` cuando el clip contiene al calamar completo y no solo el chorro.
- `PlayerVisualStateController` reproduce `InkPulse.anim` una vez y ajusta la velocidad del clip a `InkPulseController.PulseDuration`.
- `PlayerVisualStateController` reproduce `PortalEffect.anim` antes de que `ScenePortal` cargue la escena destino.
- La escala y posicion de `SquidVisual` dimensionan el calamar; la escala y posicion de `InkPulseVisual` y `PortalVisual` dimensionan exclusivamente sus efectos.
- Los clips que animan el `SpriteRenderer` del mismo objeto donde vive su `Animator` deben tener binding path vacio.
- `Squid.controller` no debe contener transiciones hacia Ink-Pulse; ese efecto pertenece al controller separado `InkPulseVisual.controller`.

## Reglas de separacion

- Las animaciones deben vivir preferentemente en hijos visuales, no en el root que gobierna logica.
- No animar `PlayerBoundaries` ni `CameraBoundaries`.
- No usar animaciones para corregir colliders que deberian estar ajustados en prefab o escena.
- `PezGlobo.anim` debe quedar sin loop: `PufferfishEnemy` la reproduce una sola vez al comenzar la hinchazon.
- En `BossNetWall`, las capas visuales pueden animarse, pero la altura de gameplay sigue viniendo de `PlayerBoundaries`.
- En UI, los managers actualizan estado; las animaciones solo presentan transicion o feedback.

## Checklist antes de commit

- El clip y/o controller estan en la carpeta de dominio correcta.
- El nombre cumple convencion.
- Se incluye el `.meta` de todo asset nuevo.
- La integracion funciona en la escena runtime objetivo.
- No se suben archivos fuente externos al runtime del juego.
- La animacion no introduce dependencias de escena dentro de un prefab.

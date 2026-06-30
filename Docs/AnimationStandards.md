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
- Cuerpo: `Assets/Content/Animations/Characters/BabySquid/default/Movement/Squid.controller`
- Efecto Ink-Pulse: `Assets/Content/Animations/Characters/BabySquid/default/InkPulse/InkPulseVisual.controller`
- Efecto portal: `Assets/Content/Animations/Characters/BabySquid/default/PortalEffect/PortalEffect.controller`

Clips:
- `default/Movement/Movement.anim`: animacion base de movimiento.
- `default/InkPulse/InkPulse.anim`: impulso visual de tinta.
- `default/PortalEffect/PortalEffect.anim`: transicion visual previa al cambio de escena.

Reglas:
- El root de prefab `BabySquid` gobierna estado y gameplay; en escena su instancia puede llamarse `Squid`.
- El root no debe usar animaciones para modificar colliders, posicion de gameplay ni input.
- `SkinMount` es el punto runtime donde se instancia la skin equipada, cuando el catalogo define `playerSkinPrefabResourcePath`.
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

## Skins de BabySquid

Cada skin jugable es un prefab visual, no un prefab alternativo de jugador. Debe cargarse desde una carpeta `Resources` mediante `playerSkinPrefabResourcePath` en `unlockables-catalog.json`.

Contrato minimo del prefab de skin:

```text
SkinNombre
|- MovementVisual o SquidVisual
|- InkPulseVisual
`- PortalVisual
```

Reglas:
- Las animaciones jugables de skins viven bajo `Assets/Content/Animations/Characters/BabySquid/<Skin>/`.
- La skin base vive en `Assets/Content/Animations/Characters/BabySquid/default/`.
- Cada carpeta de skin contiene `Movement/` como fuente visual y `Generated/` como salida tecnica regenerable.
- `default` tambien contiene fuentes separadas `InkPulse/` y `PortalEffect/`, porque la skin base ya tiene animaciones propias para esos estados.
- El prefab de skin puede tener `PlayerSkinVisualSet` en el root. Si no lo tiene, `PlayerSkinApplier` lo agrega en runtime y resuelve hijos por nombre.
- `MovementVisual` o `SquidVisual` representa la animacion normal de movimiento de esa skin.
- `InkPulseVisual` representa la animacion propia de Ink-Pulse de esa skin.
- `PortalVisual` representa la animacion propia de portal de esa skin.
- Cada raiz visual puede tener su propio `Animator` y su propio `Animator Controller`.
- Si una skin usa nombres de estado o clip distintos a `InkPulse`, `Portal` y `PortalEffect`, esos nombres deben declararse en `PlayerSkinVisualSet`.
- El prefab de skin no debe tener `Rigidbody2D`, colliders de gameplay, `PlayerMovement`, `InkPulseController`, `PlayerCollision`, `ShrimpCollector` ni scripts de economia.
- La compra y eleccion de skin viven en el perfil; la aplicacion visual runtime la hace `PlayerSkinApplier` sobre `SkinMount`.
- Si la ruta del catalogo esta vacia, el prefab no existe o falta alguna de las tres raices visuales, se mantiene el visual base de `BabySquid`.

Prefabs jugables actuales:
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Default.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Chile.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Formal.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Huaso.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Marley.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Nemo.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Rock.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Sonic.prefab`
- `Assets/Content/Prefabs/Player/Resources/PlayerSkins/Travis.prefab`

`Tools/Squid/Player/Build Skin Prefabs` ejecuta `PlayerSkinAssetBuilder` y regenera esos prefabs desde `Assets/Content/Animations/Characters/BabySquid/`. La skin base usa `Assets/Content/Animations/Characters/BabySquid/default/Generated` como carpeta generada y toma sus sprites fuente desde `default/Movement`, `default/InkPulse` y `default/PortalEffect`. Mientras una skin alternativa no tenga secuencias separadas para `InkPulse` y `Portal`, la utilidad usa la secuencia disponible de la skin como placeholder tecnico para las tres raices visuales.

Skins activas en catalogo runtime:
- `skin.default` -> `PlayerSkins/Default`
- `skin.bob_marley` -> `PlayerSkins/Marley`
- `skin.rockstar` -> `PlayerSkins/Rock`
- `skin.formal` -> `PlayerSkins/Formal`
- `skin.sonic` -> `PlayerSkins/Sonic`
- `skin.huaso` -> `PlayerSkins/Huaso`
- `skin.chile` -> `PlayerSkins/Chile`
- `skin.nemo` -> `PlayerSkins/Nemo`
- `skin.travis` -> `PlayerSkins/Travis`

Las skins definidas en fuentes de tienda pero sin carpeta jugable bajo `BabySquid/<Skin>` quedan fuera de `unlockables-catalog.json` hasta que tengan prefab y animacion runtime completos.

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

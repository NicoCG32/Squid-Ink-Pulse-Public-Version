# UI y menu

## Alcance

Este documento agrupa menu principal, pausa, game over, tienda, HUD y animaciones vinculadas a UI.

## Regla de propiedad visual

La jerarquia visual pertenece a la escena o al prefab UI. Los managers no deben autogenerar canvas, textos, slots o botones cuando esos nodos ya existen en escena.

Responsabilidad de managers:
- cablear referencias;
- mostrar u ocultar estados;
- actualizar textos e iconos;
- resolver input y eventos.

Responsabilidad de escena/prefab:
- estructura visual;
- layout;
- TextMeshPro;
- imagenes;
- canvas groups;
- orden y anchors.

Prefabs UI:
- `Assets/Content/Prefabs/UI/HUD/`: piezas de HUD reutilizables.
- `Assets/Content/Prefabs/UI/Menus/`: vistas de menus y overlays.
- Los prefabs de vista no guardan referencias a managers, jugador ni sesion.
- Los botones dentro de prefabs de vista no deben depender de eventos persistentes; el manager correspondiente cablea los listeners en runtime.

## Menu principal

Archivo: `Assets/Implementation/Code/MainMenu/MainMenu.cs`

Responsabilidad:
- Gestionar navegacion del menu principal.
- Conectar acciones de inicio, salida o navegacion a escenas.
- Cargar la escena de juego mediante ruta estable del asset.
- Cargar `Assets/Scenes/OptionsMenu/OptionsMenu.unity` desde el boton de opciones.

## Pausa

Archivos:
- `Assets/Implementation/Code/UI/Pause/PauseMenuManager.cs`
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`

Responsabilidad:
- Abrir y cerrar pausa.
- Coordinar animacion de botones.
- Mantener efecto visual mientras el juego esta pausado.
- Reanudar el juego solo despues de terminar la animacion de cierre.

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab`
- Root esperado: `PauseCanvas`.
- `PauseMenuManager` puede resolver automaticamente `PauseCanvas`, `CanvasGroup`, botones y elementos animados si la vista existe como hija del manager.

Input:
- `P` o `Esc` alternan el menu de pausa.

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar estado de derrota.
- Cubrir toda el area visible del canvas.
- Ofrecer navegacion hacia reinicio o menu principal.

El overlay oscuro debe comportarse como pantalla completa, no como subventana.
`Reintentar` inicia una run nueva desde `SceneFlowController.primaryGameplaySceneName`, por defecto `ZonaEpipelagica`, incluso si la derrota ocurrio en `ZonaAbisopelagica`.

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab`
- Root esperado: `GameOverCanvas`.
- `GameOverMenuManager` puede resolver automaticamente `GameOverCanvas`, `CanvasGroup`, botones y elementos animados si la vista existe como hija del manager.

## Tienda temporal

Archivo: `Assets/Implementation/Code/UI/Shop/InGameShopManager.cs`

Responsabilidad:
- Abrir overlay interactuable al tocar `DealerFish`.
- Mostrar imagen del prefab de gadget seleccionado aleatoriamente.
- Mostrar precio y contador de expiracion.
- Permitir compra con camarones mediante `B` o click sobre el boton `Comprar`.
- Cerrar automaticamente al agotarse el tiempo.

Reglas:
- El canvas de tienda pertenece a la escena o a un prefab de vista instanciado bajo el manager.
- El nodo manager vive en `UI/InGameShopManager`.
- Prefab de vista: `Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab`.
- Root esperado: `InGameCanvas`.
- El contador usa tiempo real cuando `pauseGameplayWhileOpen` esta activo.
- Los textos `B` y `Precio` pulsan para llamar la atencion.
- `SinSaldo` aparece solo despues de intentar comprar sin saldo.
- `Comprar` debe tener componente `Button`; `InGameShopManager` cablea en runtime su accion hacia `BuyCurrentOffer`.
- No hay boton de salir: el cierre ocurre por tiempo o compra.
- Las ofertas se filtran por `RunGadgetUnlockService`; un gadget no habilitado por hitos no aparece.
- Esta tienda es la unica que vende gadgets.

## Tienda out-of-game

Escena prevista: `ShopMenu`

Servicios de dominio:
- `PermanentShopService`
- `UnlockablesCatalogQuery`
- `PermanentUpgradeEffectResolver`

Responsabilidad:
- Mostrar subtienda de skins.
- Mostrar subtienda de mejoras permanentes.
- Consumir camarones persistentes mediante transacciones de `PermanentShopService`.
- Mostrar estados derivados: bloqueado por meta, sin saldo, ya comprado, nivel maximo o compra exitosa.

Reglas:
- No vende gadgets. Los gadgets son compras de run mediante `DealerFish`.
- No descuenta camarones directamente desde botones.
- No modifica `player-profile.json` de forma directa.
- No calcula precios por su cuenta; usa `PermanentShopService.GetPermanentUpgradePrice()` o datos del catalogo cuando corresponda.
- La aplicacion visual de skins debe modificar visuales del jugador, no movimiento, colision ni reglas de Ink-Pulse.

## HUD

Archivos:
- `Assets/Implementation/Code/UI/GameUIRoot.cs`
- `Assets/Implementation/Code/UI/HUD/ChargeBar.cs`
- `Assets/Implementation/Code/UI/HUD/InkBarFillPresenter.cs`
- `Assets/Implementation/Code/UI/HUD/ShrimpCounterDisplay.cs`
- `Assets/Implementation/Code/UI/HUD/ScoreCounterDisplay.cs`
- `Assets/Implementation/Code/UI/HUD/GadgetInventoryHud.cs`

## GameUIRoot

`GameUIRoot` es el contrato de composicion de UI de una escena jugable. No gobierna gameplay, no instancia prefabs y no decide estados; solo agrupa y expone referencias hacia la UI declarada en escena.

Estructura esperada en `ZonaEpipelagica` y `ZonaAbisopelagica`:

```text
GameUIRoot
|- EventSystem
|- HUD
|  |- InkBar
|  |- GadgetSlots
|  |- ShrimpCounter
|  \- Score
|- PauseMenuManager
|  \- PauseCanvas
|- GameOverMenuManager
|  \- GameOverCanvas
\- InGameShopManager
   \- InGameCanvas
```

Reglas:
- `GameUIRoot` puede tener referencias a vistas, HUD y managers UI.
- Los managers siguen siendo duenos del comportamiento de pausa, game over y tienda.
- Los prefabs de vista no deben contener managers ni referencias a sesion.
- Si se reestructura la UI, se debe actualizar `GameUIRoot` y luego validar con la utilidad de editor.

Responsabilidad:
- Mostrar carga del Ink-Pulse.
- Mostrar score runtime de la run.
- Mostrar el saldo de camarones del perfil persistente mediante `ShrimpRuntimeWallet`.
- Mostrar gadgets con su icono dentro del hueco `GadgetN`.
- Mostrar tecla solo si el gadget del hueco es activo: `Q` en `Gadget1`, `W` en `Gadget2`.

Contrato de barra Ink-Pulse:
- `ChargeBar` es la fachada consumida por `InkPulseController`. Solo recibe un valor normalizado y lo replica al presenter visual o al slider legacy.
- `InkBarFillPresenter` es la especializacion visual de barras modernas. No conoce sesion, jugador, Ink-Pulse ni progresion; solo traduce un valor normalizado a layout.
- `EffectPresentationMode.FollowFillTip` mueve `EffectAnchor` hacia la punta del relleno. Es la variante vertical usada en `ZonaAbisopelagica`.
- `EffectPresentationMode.RevealThroughFill` deja `InkBarEffectVisual` espacialmente fijo y usa `Fill` como mascara invisible. Es la variante horizontal/rotada usada en `ZonaEpipelagica`.
- `ZonaTutorial` conserva `InkPulseBar` legacy con `Slider` y `ChargeBar` sin presenter. Esto se mantiene deliberadamente hasta redisenar el tutorial.

Prefabs disponibles:
- `Assets/Content/Prefabs/UI/HUD/InkBarHorizontal.prefab`: fuente para `ZonaEpipelagica`.
- `Assets/Content/Prefabs/UI/HUD/InkBarVertical.prefab`: fuente para `ZonaAbisopelagica`.
- `Assets/Content/Prefabs/UI/HUD/InkPulseBarLegacy.prefab`: fuente legacy para `ZonaTutorial`.
- `Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab`: slots de gadgets activos/pasivos.
- `Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab`: contador de camarones persistentes.
- `Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab`: puntaje runtime.
- `ZonaEpipelagica`, `ZonaAbisopelagica` y `ZonaTutorial` usan estas piezas como instancias prefab. Las escenas pueden conservar overrides de posicion, rotacion y escala. El prefab debe conservar jerarquia interna, imagenes, animador, mascara y componentes `ChargeBar`/`InkBarFillPresenter` cuando corresponda.

Nota sobre Tutorial:
- La estructura es igual, salvo que el HUD contiene `InkPulseBar` en vez de `InkBar`.
- Esta diferencia esta validada explicitamente por `SceneContractValidator`; no debe resolverse renombrando nodos sin actualizar el contrato.

Regla visual de inventario:
- Los slots no tienen gadget fijo en escena; se llenan por orden de adquisicion.
- El icono viene de la oferta comprada y queda registrado en `RuntimeGadgetInventory`.
- Los pasivos ocupan hueco visual, pero no exponen tecla.
- La posesion de gadgets es unica; el HUD no muestra cantidades.
- Las letras visibles de slots activos pulsan con la misma logica de atencion usada por la tienda.
- `GadgetInventoryHud` no crea textos ni imagenes de slot en runtime.

## UI decorativa

Archivos:
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`
- `Assets/Implementation/Code/UI/MenuScreenAnimation.cs`

Responsabilidad:
- Dar vida a pantallas de menu sin mezclar animacion con navegacion o sesion.
- `MenuButtonAnimation` puede estar en el boton o en un hijo visual/textual; en ambos casos anima el `Button` padre si existe.
- `MenuButtonAnimation` no expone parametros por boton; si se requiere balancear esa animacion, debe moverse a un manager/controlador de UI.

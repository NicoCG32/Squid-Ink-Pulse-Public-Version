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
- `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab`: overlay narrativo para inicio, portales y derrota.
- Los prefabs de vista no guardan referencias a managers, jugador ni sesion.
- Los botones no deben guardar referencias persistentes hacia managers externos de escena dentro del prefab.
- Si el destino vive dentro del mismo prefab, un `OnClick` persistente es valido y auditable.
- Si el destino vive en escena, la conexion debe quedar visible por Inspector o como referencia serializada del manager; cualquier cableado runtime debe ser respaldo defensivo documentado, no el contrato principal.

## Menu principal

Archivo: `Assets/Implementation/Code/MainMenu/MainMenu.cs`

Responsabilidad:
- Gestionar navegacion del menu principal.
- Conectar acciones de inicio, salida o navegacion a escenas.
- Cargar la escena de juego mediante ruta estable del asset.
- Abrir `OptionsMenu` como prefab/panel asignado en la escena, no como escena independiente.
- Cargar `Assets/Scenes/ShopMenu/ShopMenu.unity` desde el boton de tienda.
- Mostrar el comic de inicio mediante `LoreComicPresenter.PlayGameStartIfAvailable()` antes de cargar gameplay cuando exista `LoreComicRoot` activo.

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
- No exponer ni cablear boton `Salir`; esa accion pertenece solo al menu principal.

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab`
- Root esperado: `PauseCanvas`.
- `PauseMenuManager` puede resolver automaticamente `PauseCanvas`, `CanvasGroup`, botones y elementos animados si la vista existe como hija del manager.

Input:
- `P` o `Esc` alternan el menu de pausa.

Contrato de eventos:
- Los `OnClick` persistentes configurados en Inspector no deben apagarse en runtime.
- El cableado runtime de `PauseMenuManager` es respaldo defensivo y no reemplaza la auditoria visual/serializada del Inspector.

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar estado de derrota.
- Cubrir toda el area visible del canvas.
- Ofrecer navegacion hacia reinicio o menu principal.
- Esperar el comic de derrota de `LoreComicPresenter.PlayDefeatIfAvailable()` antes de mostrar la vista de Game Over cuando exista una entrada valida.

El overlay oscuro debe comportarse como pantalla completa, no como subventana.
`Reintentar` inicia una run nueva desde `SceneFlowController.primaryGameplaySceneName`, por defecto `ZonaEpipelagica`, incluso si la derrota ocurrio en `ZonaAbisopelagica`.

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab`
- Root esperado: `GameOverCanvas`.
- `GameOverMenuManager` puede resolver automaticamente `GameOverCanvas`, `CanvasGroup`, botones y elementos animados si la vista existe como hija del manager.
- Si existen los textos `PuntajeObtenido` y `MaximoPuntaje` / `MáximoPuntaje`, `GameOverMenuManager` los rellena con el puntaje final de la run y el mejor puntaje persistente actualizado.
- El manager no calcula score ni modifica la estetica de esos textos; solo consume `RuntimeRunScore.LastCompletedScore` y `PersistentPlayerProfile.BestScore`.

## Lore comics

Archivo: `Assets/Implementation/Code/Lore/LoreComicPresenter.cs`

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab`
- Root esperado: `LoreComicRoot`.
- Nodo visual esperado: `Comic`.

Responsabilidad:
- Mostrar vinetas narrativas de inicio, portal, derrota y tienda in-game.
- Oscurecer el fondo mediante `Dimmer`.
- Asignar el sprite de vineta sobre `Vineta`.
- Esperar duracion en tiempo real y, si corresponde, boton de continuar.
- Pausar temporalmente `Time.timeScale` mientras el comic esta visible si `pauseTimeWhileShowing` esta activo.

Reglas:
- Runtime no crea Canvas, botones, textos ni jerarquias visuales.
- `LoreComicPresenter` solo activa/desactiva referencias existentes y asigna sprites configurados en `entries`.
- `ContinuarBoton` sigue el contrato de botones: nodo con `Button` y `Visual`.
- El prefab completo debe estar en layer `UI`.
- Las vinetas viven en `Assets/Content/Art/ComicLore/` separadas por dominio. Se reemplazan desde Inspector o prefab conservando sus `.meta`.
- Las instalaciones base viven en `MainMenu` y en los prefabs `GameRoot_ZonaEpipelagica`, `GameRoot_ZonaAbisopelagica` y `GameRoot_ZonaTutorial`.
- Tienda in-game usa tres eventos: `ShopInGameFirst`, `ShopInGameLastPurchased` y `ShopInGameLastNoPurchase`.

## Tienda temporal

Archivo: `Assets/Implementation/Code/UI/Shop/InGameShopManager.cs`

Responsabilidad:
- Abrir overlay interactuable al tocar `DealerFish`.
- Mostrar imagen del prefab de gadget seleccionado aleatoriamente.
- Mostrar precio y contador de expiracion.
- Permitir compra con camarones mediante `B` o click sobre el boton `Comprar`.
- Cerrar automaticamente al agotarse el tiempo.
- Coordinar comics de primera entrada/salida cuando se abre desde `DealerFish`.

Reglas:
- El canvas de tienda pertenece a la escena o a un prefab de vista instanciado bajo el manager.
- El nodo manager vive en `UI/InGameShopManager`.
- Prefab de vista: `Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab`.
- Root esperado: `InGameCanvas`.
- El contador usa tiempo real cuando `pauseGameplayWhileOpen` esta activo.
- Los textos `B` y `Precio` pulsan para llamar la atencion.
- `SinSaldo` aparece solo despues de intentar comprar sin saldo.
- `Comprar` debe tener componente `Button` y accion auditable hacia `BuyCurrentOffer`; si `InGameShopManager` cablea en runtime, debe tratarse como respaldo defensivo, no como contrato principal.
- `InGameShopManager` no debe desactivar listeners persistentes del Inspector.
- No hay boton de salir: el cierre ocurre por tiempo o compra.
- Las ofertas se filtran por `RunGadgetUnlockService`; un gadget no habilitado por hitos no aparece.
- Esta tienda es la unica que vende gadgets.
- La primera salida de tienda desde `DealerFish` muestra comic de compra o no compra segun si se adquirio un gadget; las aperturas posteriores de la run no repiten esos comics.

## Tienda out-of-game

Escena: `Assets/Scenes/ShopMenu/ShopMenu.unity`.

Componente de escena: `Assets/Implementation/Code/UI/Shop/OutOfGameShopManager.cs`, serializado en `ShopMenu/Canvas`.

Servicios de dominio:
- `PermanentShopService`
- `UnlockablesCatalogQuery`
- `PermanentUpgradeEffectResolver`

Responsabilidad:
- Mostrar subtienda de skins.
- Mostrar subtienda de mejoras permanentes.
- Consumir camarones persistentes mediante transacciones de `PermanentShopService`.
- Mostrar estados derivados: bloqueado por meta, sin saldo, ya comprado, nivel maximo o compra exitosa.

Contrato de interaccion actual:

```text
ShopMenu
|- Canvas
|  |- Panel
|  |  |- arte del mueble y decoracion (autoria visual)
|  |  `- ShopInteractionRoot
|  |     |- Upgrade01Boton ... Upgrade04Boton
|  |     |- Skin01Boton ... Skin04Boton
|  |     |- SkinAnteriorBoton
|  |     `- SkinSiguienteBoton
|  `- ShrimpCounter (instancia del prefab)
`- OptionsMenu (instancia del prefab global, root de escena)
```

- Cada control dentro de `ShopInteractionRoot` conserva el contrato `*Boton/Button/Visual/{Normal,Destacado,Presionado}`.
- El `Image` del hijo `Button` es una hitbox blanca con alpha `0`; no es arte ni debe recolorearse.
- Los hitboxes de `ShopInteractionRoot` usan el SFX generico de presion `Assets/Content/Audio/SFX/MainMenu/Splat.mp3`. Este es un contrato funcional temporal: puede reemplazarse por un SFX de tienda aprobado sin tocar la jerarquia ni los sprites.
- El usuario ajusta la posicion y el tamano de cada owner `*Boton` para coincidir con las vitrinas o flechas del mueble. El codigo no modifica sus sprites, layout, escala, color ni jerarquia visual.
- Los cuatro slots superiores estan reservados, en este orden, para `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.
- Los cuatro slots inferiores muestran una pagina de skins. El catalogo actual contiene solo `skin.default`; la paginacion queda preparada para futuras skins sin duplicar la tienda.
- Los campos TMP de detalle del manager son opcionales y aun no estan asignados, porque su presentacion visual pertenece al trabajo de UI. La compra y seleccion funcionan aunque esos campos sigan vacios.

Eventos persistentes requeridos:

- `Upgrade01Boton` a `Upgrade04Boton` llaman `OutOfGameShopManager.SelectUpgradeSlot(int)` con indices `0` a `3`.
- `Skin01Boton` a `Skin04Boton` llaman `OutOfGameShopManager.SelectSkinSlot(int)` con indices `0` a `3`.
- `SkinAnteriorBoton` llama `PreviousSkinPage()` y `SkinSiguienteBoton` llama `NextSkinPage()`.
- `ComprarBoton/Button` llama `PurchaseSelected()`.
- `VolverBoton/Button` llama `MainMenu.VolverAlMenuPrincipal()`.
- El prefab `OptionsMenu` existe como root separado de escena para no heredar escala de `Canvas` ni del mueble. No se agrega ni se corrige por codigo runtime.

Reglas:
- No vende gadgets. Los gadgets son compras de run mediante `DealerFish`.
- No descuenta camarones directamente desde botones.
- No modifica `player-profile.json` de forma directa.
- No calcula precios por su cuenta; usa `PermanentShopService.GetPermanentUpgradePrice()` o datos del catalogo cuando corresponda.
- La aplicacion visual de skins debe modificar visuales del jugador, no movimiento, colision ni reglas de Ink-Pulse.
- Comprar una skin la desbloquea; una segunda accion sobre una skin ya poseida la equipa. El cambio visual efectivo de la skin es una fase posterior y no debe alterar controladores de gameplay.

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

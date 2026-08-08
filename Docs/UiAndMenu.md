# UI y menú

## Alcance

Este documento agrupa menú principal, pausa, game over, tienda, HUD y animaciones vinculadas a UI.

## Regla de propiedad visual

La jerarquía visual pertenece a la escena o al prefab UI. Los managers no deben autogenerar canvas, textos, slots o botones cuando esos nodos ya existen en escena.

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
- Los prefabs de vista no guardan referencias a managers, jugador ni sesión.
- Los botones no deben guardar referencias persistentes hacia managers externos de escena dentro del prefab.
- Si el destino vive dentro del mismo prefab, un `OnClick` persistente es valido y auditable.
- Si el destino vive en escena, la conexión debe quedar visible por Inspector o como referencia serializada del manager; cualquier cableado runtime debe ser respaldo defensivo documentado, no el contrato principal.

## Menú principal

Archivo: `Assets/Implementation/Code/MainMenu/MainMenu.cs`

Responsabilidad:
- Gestionar navegación del menú principal.
- Conectar acciones de inicio, salida o navegación a escenas.
- Cargar la escena de juego mediante ruta estable del asset.
- Abrir `OptionsMenu` como prefab/panel asignado en la escena, no como escena independiente.
- Cargar `Assets/Scenes/ShopMenu/ShopMenu.unity` desde el boton de tienda.
- Mostrar el comic de inicio mediante `LoreComicPresenter.PlayGameStartIfAvailable()` antes de cargar gameplay cuando exista `LoreComicRoot` activo.
- Escuchar el código de muestra `SONICYNOTA7` sin campo de texto y acreditar camarones de prueba para probar tienda. El código no se consume: cada ingreso completo suma otros `676700` camarones. El atajo se usa desde `MainMenu` y tambien desde `ShopMenu` cuando la escena conserva el componente de navegación de menú.

## OptionsMenu global

Archivos:
- `Assets/Implementation/Code/UI/Options/OptionsMenuManager.cs`
- `Assets/Implementation/Code/Audio/GlobalAudioSettings.cs`
- `Assets/Implementation/Code/Core/Lifecycle/MobilePersistenceCheckpoint.cs`

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/OptionsMenu.prefab`
- Root esperado: `OptionsMenu`.
- Canvas interno esperado: hijo `Canvas`.
- Panel funcional esperado: `OptionsPanel`.
- Fondo oscuro esperado: `Background` o `Fondo`.

Responsabilidad:
- Mostrar y ocultar opciones de volumen, resolucion y pantalla completa.
- Guardar configuración general en `PlayerPrefs`, no en la base persistente `db`.
- Mantener el Canvas con sorting alto para que se vea sobre MainMenu, ShopMenu y escenas jugables.
- Activar o desactivar el fondo visual existente junto con el menú.
- Aplicar el volumen maestro a todo el audio del juego, incluyendo soundtrack normal, soundtrack Ink-Pulse, SFX de botones, sonidos de inkbar y cualquier `AudioSource` que no tenga mixer asignado.

Reglas:
- El fondo oscuro es autoria visual del prefab. El script no crea `Image`, Canvas ni jerarquía de fondo.
- `OptionsMenuManager` solo busca un hijo directo llamado `Background` o `Fondo` bajo `OptionsPanel` o bajo el Canvas propietario si la referencia no esta serializada.
- El fondo debe quedar detras del contenido interactivo. Si vive bajo `OptionsPanel`, el manager lo mantiene como primer hermano.
- El prefab se instancia como root de escena separado. No debe quedar dentro del Canvas del mueble de ShopMenu ni dentro de paneles decorativos.
- Si algún Canvas o root fue guardado accidentalmente con escala local `0`, el manager restaura escala `(1,1,1)` en runtime como defensa técnica; esto no sustituye la correccion visual en prefab.
- `GlobalAudioSettings` aplica el volumen guardado antes de cargar escena mediante `AudioListener.volume`; por eso el control afecta audio con mixer y audio sin mixer.
- Si existe un `AudioMixer` con parametro expuesto, `OptionsMenuManager` tambien sincroniza ese parametro para conservar compatibilidad con mezclas futuras.
- El slider de volumen guarda `MasterVolume` en `PlayerPrefs`. No debe escribir en `player-profile.json`, `player-records.json` ni `local-leaderboard.json`.
- Las escrituras conocidas de volumen, URL de feria y opciones de escritorio se confirman inmediatamente mediante `PlayerPreferencesCheckpoint`. Cualquier preferencia que permanezca pendiente se vacía una sola vez cuando el player móvil entra en pausa o pierde el foco.
- El checkpoint de `PlayerPrefs` no serializa estado transitorio de una run ni vuelve a guardar la base JSON: sus repositorios conservan su política de escritura inmediata y atómica.

## Pausa

Archivos:
- `Assets/Implementation/Code/UI/Pause/PauseMenuManager.cs`
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`

Responsabilidad:
- Abrir y cerrar pausa.
- Coordinar animación de botones.
- Mantener efecto visual mientras el juego esta pausado.
- Reanudar el juego solo despues de terminar la animación de cierre.
- No exponer ni cablear boton `Salir`; esa acción pertenece solo al menú principal.

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/PauseMenu.prefab`
- Root esperado: `PauseCanvas`.
- `PauseMenuManager` puede resolver automaticamente `PauseCanvas`, `CanvasGroup`, botones y elementos animados si la vista existe como hija del manager.

Input:
- `P` o `Esc` alternan el menú de pausa.

Contrato de eventos:
- Los `OnClick` persistentes configurados en Inspector no deben apagarse en runtime.
- El cableado runtime de `PauseMenuManager` es respaldo defensivo y no reemplaza la auditoria visual/serializada del Inspector.

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar estado de derrota.
- Cubrir toda el area visible del canvas.
- Ofrecer navegación hacia reinicio o menú principal.
- Esperar el comic de derrota de `LoreComicPresenter.PlayDefeatIfAvailable()` antes de mostrar la vista de Game Over cuando exista una entrada valida.

El overlay oscuro debe comportarse como pantalla completa, no como subventana.
`Reintentar` inicia una run nueva desde `SceneFlowController.primaryGameplaySceneName`, por defecto `ZonaEpipelagica`, incluso si la derrota ocurrio en `ZonaAbisopelagica`.

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/GameOverMenu.prefab`
- Root esperado: `GameOverCanvas`.
- `GameOverMenuManager` puede resolver automaticamente `GameOverCanvas`, `CanvasGroup`, botones y elementos animados si la vista existe como hija del manager.
- Si existen los textos `PuntajeObtenido` y `MaximoPuntaje`, `GameOverMenuManager` los rellena con el puntaje final de la run y el mejor puntaje persistente actualizado.
- El manager no calcula score ni modifica la estetica de esos textos; solo consume `RuntimeRunScore.LastCompletedScore` y `PersistentPlayerProfile.BestScore`.

## Lore comics

Archivo: `Assets/Implementation/Code/Lore/LoreComicPresenter.cs`

Prefab de vista:
- `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab`
- Root esperado: `LoreComicRoot`.
- Nodo visual esperado: `Comic`.

Responsabilidad:
- Mostrar viñetas narrativas de inicio, portal, derrota y tienda in-game.
- Oscurecer el fondo mediante `Dimmer`.
- Asignar el sprite de vineta sobre `Vineta`.
- Esperar duracion en tiempo real y, si corresponde, boton de continuar.
- Pausar temporalmente `Time.timeScale` mientras el comic esta visible si `pauseTimeWhileShowing` esta activo.

Reglas:
- Runtime no crea Canvas, botones, textos ni jerarquias visuales.
- `LoreComicPresenter` orquesta reproduccion, pausa y persistencia; `LoreComicView` activa/desactiva referencias existentes y asigna sprites configurados en `entries`.
- `LoreComicEntrySelector` decide la entrada por evento/zona y `LoreComicPersistencePolicy` decide que eventos se marcan como vistos.
- `ContinuarBoton` sigue el contrato de botones: nodo con `Button` y `Visual`.
- El prefab completo debe estar en layer `UI`.
- Las viñetas viven en `Assets/Content/Art/ComicLore/` separadas por dominio. Se reemplazan desde Inspector o prefab conservando sus `.meta`.
- Las instalaciones base viven en `MainMenu` y en los prefabs `GameRoot_ZonaEpipelagica` y `GameRoot_ZonaAbisopelagica`.
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
- El canvas de tienda pertenece a la escena o a un prefab de vista referenciado por el `GameRoot` de cada zona.
- El manager recibe sus referencias de vista por campos serializados; no busca `InGameCanvas`, `Gadget`, `Precio`, `B`, `Comprar`, `SinSaldo` ni `Timer` por nombre en runtime.
- Prefab de vista: `Assets/Content/Prefabs/UI/Menus/InGameShopMenu.prefab`.
- Root esperado de la vista: `InGameCanvas`, asignado en `menuRoot`.
- `timerText` es opcional mientras el prefab de vista no exponga un nodo visible de contador.
- El contador usa tiempo real cuando `pauseGameplayWhileOpen` esta activo.
- Los textos `B` y `Precio` pulsan para llamar la atencion.
- `SinSaldo` aparece solo despues de intentar comprar sin saldo.
- `Comprar` debe tener componente `Button`. `InGameShopManager` registra en runtime la accion `BuyCurrentOffer` sobre la referencia serializada, sin depender de una busqueda jerarquica.
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
- Mostrar estados derivados: bloqueado por meta, sin saldo, ya adquirido, equipado, nivel maximo o compra exitosa.
- Poblar nombre, descripcion y precio del producto seleccionado desde el catalogo.
- Mostrar precios compactos mediante la misma nomenclatura de `ShrimpCounterDisplay`: `1000` se muestra como `1K`, `1500` como `1.5K`, `1000000` como `1M`.

Atajo de tienda:
- En `ShopMenu`, escribir `SONICYNOTA7` directamente con el teclado acredita `676700` camarones por ingreso completo. Se usa para probar compras, mejoras y skins sin depender de farmeo.

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
- Los hitboxes de `ShopInteractionRoot` usan el SFX generico de presion `Assets/Content/Audio/SFX/MainMenu/Splat.mp3`. Este contrato permite reemplazar el SFX de tienda sin tocar la jerarquía ni los sprites.
- La autoria visual de cada owner `*Boton` pertenece a la escena: posición, tamano, sprites, layout, escala, color y jerarquía no se modifican por código.
- Los cuatro slots superiores estan reservados, en este orden, para `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.
- Las descripciones de mejoras deben ser cortas y con tono de tienda del juego, sin tildes ni caracteres especiales:
  - Tinta Persistente: "Tu nube aguanta mas: entra, limpia el peligro y sal con estilo."
  - Pulso Recargado: "Menos espera entre pulsos; mas escapes al limite."
  - Botin de Camarones: "Cada camaron rinde mas cuando el oceano se pone pesado."
  - Gloria Marina: "Cada maniobra peligrosa deja una historia mas grande."
- Los cuatro slots inferiores muestran una pagina de skins tomada desde `unlockables-catalog.json`.
- Las imagenes de tienda se cargan con `Resources.Load<Sprite>()` desde rutas sin extension declaradas en el catalogo. La raiz actual es `Assets/Content/Art/UI/ShopMenu/Resources/ShopMenu/`.
- Las mejoras usan `shopSpriteResourcePath` para estado normal y `shopHighlightedSpriteResourcePath` para el estado visual seleccionado. En el arte actual, los sprites seleccionados terminan en `Ink`.
- Las skins usan `shopSpriteResourcePath` para su imagen base, `shopBuyedSpriteResourcePath` para el estado comprado no equipado y `shopSelectedSpriteResourcePath` para la skin equipada. Si faltan sprites de comprado o equipado, el manager conserva fallback hacia la imagen base.
- Los campos TMP de detalle `NombreProducto`, `DescripcionProducto` y `PrecioProducto` son referencias funcionales del manager. Su posición y estilo pertenecen a la escena; el manager solo escribe contenido.
- El indicador de nivel de mejoras vive en el nodo visual `Mejorable/Gota1..Gota5`. Cada gota debe exponer estados `Visual/Vacia`, `Visual/Media` y `Visual/Llena`. Cinco gotas representan diez segmentos de mejora. `OutOfGameShopManager` no busca esos nombres en runtime: las gotas y sus estados quedan serializados en la escena y validados por `SceneCompositionValidator`.
- El vendedor de `ShopMenu` expone dos estados serializados: `Default` y `AfterBuy`/`Happy`. `OutOfGameShopManager` vuelve a `Default` al entrar o salir del menú, y cambia a `AfterBuy`/`Happy` solo despues de una compra real exitosa, conservando ese estado durante la visita actual. Equipar o desequipar una skin ya comprada no cuenta como compra nueva.

Eventos persistentes requeridos:

- `Upgrade01Boton` a `Upgrade04Boton` llaman `OutOfGameShopManager.SelectUpgradeSlot(int)` con indices `0` a `3`.
- `Skin01Boton` a `Skin04Boton` llaman `OutOfGameShopManager.SelectSkinSlot(int)` con indices `0` a `3`.
- `SkinAnteriorBoton` llama `PreviousSkinPage()` y `SkinSiguienteBoton` llama `NextSkinPage()`.
- `ComprarBoton/Button` llama `PurchaseSelected()`.
- `VolverBoton/Button` llama `MainMenu.VolverAlMenuPrincipal()`.
- El prefab `OptionsMenu` existe como root separado de escena para no heredar escala de `Canvas` ni del mueble. El fondo oscuro pertenece al propio prefab y se muestra u oculta por `OptionsMenuManager`.

Reglas:
- No vende gadgets. Los gadgets son compras de run mediante `DealerFish`.
- No descuenta camarones directamente desde botones.
- No modifica `player-profile.json` de forma directa.
- No calcula precios por su cuenta; usa `PermanentShopService.GetPermanentUpgradePrice()` o datos del catalogo cuando corresponda.
- La aplicacion visual de skins debe modificar visuales del jugador, no movimiento, colision ni reglas de Ink-Pulse.
- Comprar una skin la desbloquea. Una segunda acción sobre una skin ya poseida la equipa y escribe `equippedSkinId` en el perfil.
- Si la skin ya esta equipada y no es `skin.default`, accionar `Comprar` la deselecciona y vuelve a equipar `skin.default`.
- El cambio visual efectivo del jugador usa `playerSkinPrefabResourcePath`: al entrar a gameplay, `PlayerSkinApplier` instancia ese prefab bajo `BabySquid/SkinMount` y `PlayerVisualStateController` alterna sus raices `MovementVisual`/`SquidVisual`, `InkPulseVisual` y `PortalVisual`.
- El catalogo runtime solo debe incluir skins con prefab visual completo y ruta valida. Las skins conceptuales o no implementadas quedan fuera de `unlockables-catalog.json` hasta que tengan animación/prefab jugable.
- Para prueba local, las compras se guardan en `Application.persistentDataPath/db/`. Una sesión limpia de entrega tiene mejoras en `0`, camarones `0`, best `0`, leaderboard vacio y solo `skin.default` desbloqueada/equipada.

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
- Los managers siguen siendo dueños del comportamiento de pausa, game over y tienda.
- Los prefabs de vista no deben contener managers ni referencias a sesión.
- Si se reestructura la UI, se debe actualizar `GameUIRoot` y comprobar manualmente el contrato de escena.

Responsabilidad:
- Mostrar carga del Ink-Pulse.
- Mostrar score runtime de la run.
- Mostrar el saldo de camarones del perfil persistente mediante `ShrimpRuntimeWallet`.
- Mostrar gadgets con su icono dentro del hueco `GadgetN`.
- Mostrar tecla solo si el gadget del hueco es activo: `Q` en `Gadget1`, `W` en `Gadget2`.

Contrato de barra Ink-Pulse:
- `ChargeBar` es la fachada consumida por `InkPulseController`. Solo recibe un valor normalizado y lo replica al presenter visual de `InkBar`.
- `InkBarFillPresenter` es la especializacion visual de la barra actual. No conoce sesión, jugador, Ink-Pulse ni progresión; solo traduce un valor normalizado a layout.
- `EffectPresentationMode` pertenece al prefab `InkBar`; las escenas no deben elegir variantes distintas de prefab para cambiar la presentación.
- `ZonaEpipelagica` y `ZonaAbisopelagica` usan `GameRoot/GameUIRoot/HUD/InkBar`.

Prefabs disponibles:
- `Assets/Content/Prefabs/UI/HUD/InkBar.prefab`: barra Ink-Pulse canonica para todas las zonas.
- `Assets/Content/Prefabs/UI/HUD/GadgetSlots.prefab`: slots de gadgets activos/pasivos.
- `Assets/Content/Prefabs/UI/HUD/ShrimpCounter.prefab`: contador de camarones persistentes.
- `Assets/Content/Prefabs/UI/HUD/ScoreCounter.prefab`: puntaje runtime.
- `ZonaEpipelagica` y `ZonaAbisopelagica` usan estas piezas como instancias prefab. Las escenas pueden conservar overrides de posición y referencias; el prefab debe conservar jerarquía interna, imagenes, animador, mascara y componentes `ChargeBar`/`InkBarFillPresenter`.

Regla visual de inventario:
- Los slots no tienen gadget fijo en escena; se llenan por orden de adquisicion.
- El icono viene de la oferta comprada y queda registrado en `RuntimeGadgetInventory`.
- Los pasivos ocupan hueco visual, pero no exponen tecla.
- La posesion de gadgets es unica; el HUD no muestra cantidades.
- Las letras visibles de slots activos pulsan con la misma lógica de atencion usada por la tienda.
- `GadgetInventoryHud` no crea textos ni imagenes de slot en runtime.

## UI decorativa

Archivos:
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`
- `Assets/Implementation/Code/UI/MenuScreenAnimation.cs`

Responsabilidad:
- Dar vida a pantallas de menú sin mezclar animación con navegación o sesión.
- `MenuButtonAnimation` puede estar en el boton o en un hijo visual/textual; en ambos casos anima el `Button` padre si existe.
- `MenuButtonAnimation` no expone parámetros por boton; si se requiere balancear esa animación, debe moverse a un manager/controlador de UI.

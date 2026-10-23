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

## Controles touch

Archivos:
- `Assets/Implementation/Code/UI/Touch/TouchControlsVisibilityPolicy.cs`
- `Assets/Implementation/Code/UI/Touch/TouchControlsVisibilityController.cs`
- `Assets/Implementation/Code/UI/Touch/TouchSteeringSurface.cs`
- `Assets/Implementation/Code/UI/Touch/TouchSteeringCaptureState.cs`
- `Assets/Implementation/Code/UI/Touch/TouchSteeringAvailabilityPolicy.cs`
- `Assets/Implementation/Code/UI/Touch/TouchSteeringUiPolicy.cs`
- `Assets/Implementation/Code/UI/Touch/TouchGameplayCommandButton.cs`
- `Assets/Implementation/Code/UI/Touch/TouchGameplayControlsController.cs`
- `Assets/Implementation/Code/UI/Touch/TouchGameplayControlsPolicy.cs`
- `Assets/Content/Prefabs/UI/Touch/TouchControls.prefab`

La política muestra la capa de controles en Android y la oculta en players desktop o plataformas aún fuera del port. El override `showInEditor` es serializado por instancia, está apagado por defecto y sólo tiene efecto en Windows, macOS o Linux Editor; no usa `EditorPrefs`, `PlayerPrefs`, símbolos de compilación ni detección de hardware touch.

El controller debe vivir en un objeto padre activo y gobernar un root descendiente distinto. Rechaza referencias al propio owner, a sus ancestros o a objetos ajenos; recalcula en `OnEnable` para restaurar la decisión al reactivar el owner.

`TouchSteeringSurface` define ya la región de movimiento sin consultar `Touchscreen.current` ni añadir bindings globales. Sólo acepta pointer Touch del módulo UI, conserva un único dueño, ignora Down sobre controles interactivos y cancela ante sesión no jugable, tienda, overlay, pérdida de foco o lifecycle del lector. Un overlay que no pause ni cambie sesión/tienda debe llamar `SetOverlayInteractionAllowed(false)` antes de interceptar raycasts.

`TouchControls.prefab` ya compone la superficie full-stretch y cuatro botones accesibles de 160 a 240 unidades de Canvas. La superficie es el primer sibling; los hit targets de pausa, `INK-PULSO` y gadgets quedan encima, mientras textos y estados visuales no reciben raycast. El prefab no posee Canvas, `GraphicRaycaster`, EventSystem ni Collider. Cada botón muestra una etiqueta de estado explícita y sólo uno de sus pointers puede despachar al lector semántico.

`GameRoot_ZonaEpipelagica` y `GameRoot_ZonaAbisopelagica` contienen un único `SafeAreaRoot` directamente bajo `GameUIRoot/HUD`. InkBar, gadgets, camarones, score y la instancia anidada de controles viven dentro de esa raíz; `SafeAreaAdapter` transforma `Screen.safeArea` a anchors normalizados y responde a rotación o cambio de resolución sin acumular offsets. TouchControls permanece como último sibling de la raíz segura, por encima de la decoración HUD que aún recibe raycast, y no añade Canvas, `GraphicRaycaster` ni EventSystem.

Los cinco Canvas de gameplay usan referencia 1920×1080 y `matchWidthOrHeight = 1`: preservan el alto lógico en 19.5:9/20:9 y absorben el ancho adicional lateralmente. Esto evita que HUD, pausa, Game Over, tienda o cómic se recorten arriba/abajo; en 16:9 la escala efectiva no cambia. `GameRoot_ZonaTutorial` no recibe esta integración mientras siga fuera del producto activo.

La política dimensional es común a todas las zonas, incluidas las futuras: cada `GameRoot_Zona*` debe reutilizar el mismo HUD base, referencia 1920×1080, escalado por altura, raíz segura y controles. Una zona puede cambiar dinámica, enemigos, arte ambiental o mecánicas de jefe, pero no crear una política paralela de anchors, safe area o tamaños. Las diferencias visuales deben resolverse mediante contenido/prefabs compartidos y overrides mínimos justificados.

El layout de los contenedores `InkBar`, `GadgetSlots`, `Score`, `ShrimpCounter` y `TouchControlsOwner` toma `GameRoot_ZonaEpipelagica` como referencia canónica. `MobileHudLayoutContract` exige la misma geometría y fuente de prefab en las demás zonas y en las instancias de escena. Un cambio común se aplica primero en esa referencia y después en todos sus consumidores; el validador bloquea diferencias pendientes. El modo interno de llenado de InkBar conserva su especialización por zona.

Las escenas jugables conservan sustituciones locales de los cuatro elementos HUD para mantener sus referencias y efectos. Esas instancias se alojan también bajo `SafeAreaRoot`, con el mismo layout que el prefab canónico y TouchControls al final del orden de dibujo; una sustitución local no puede omitir el área segura.

Las pruebas deterministas verifican fuente anidada, cantidad de controles, raíz segura, escala por altura, referencias previas del HUD y ausencia de EventSystem o módulo duplicado. PlayMode ejerce el routing real de `InputSystemUIInputModule` con touchscreen virtual y el reemplazo Epipelágica→Abisopelágica; ergonomía multitouch y recorrido completo continúan requiriendo hardware.

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

Contrato responsive del menú:
- `ComicsTutorial` y sus tres Canvas de página usan referencia 1920×1080 con `matchWidthOrHeight = 1`. En formatos 19.5:9/20:9 conservan el alto de autoría y absorben el ancho adicional, evitando que viñetas, textos o el botón siguiente se recorten arriba o abajo.
- El `Character` animado es decoración del Main Menu. Se ancla al centro del borde derecho con pivote horizontal `0.75` y offset X cero: tres cuartas partes quedan dentro del frame y el cuarto derecho, donde convergen los tentáculos animados, permanece fuera en cualquier ancho landscape.
- Estos ajustes no cambian navegación, hitboxes ni el layout de los demás botones del menú.

El Canvas de `MainMenu` conserva la composición de referencia en móvil con `MobileCanvasScalePolicy`: match por ancho en una superficie 16:10 de tablet y por altura en 16:9 o formatos más anchos. Se recalcula si cambia la superficie; normaliza las dimensiones iniciales a landscape. En Windows conserva el escalado serializado. Esto no cambia geometría de botones, arte ni el layout de cada zona.

La escena protege el contenido con tres contenedores full-stretch y `SafeAreaAdapter`: `NavigationSafeArea` para botones, `BrandSafeArea` para el logo y `CounterSafeArea` para camarones. Conservan el orden de dibujo y no añaden Canvas ni EventSystem. Fondo y personaje siguen cubriendo el frame. En móvil, Cómo Jugar se ancla al borde izquierdo seguro, y logo/contador al derecho, conservando sus posiciones de referencia y sin acumular desplazamientos al reaplicar la presentación. Desktop mantiene los anchors y posiciones de autoría.

`MainMenuPlatformPolicy` reserva el botón Salir y su acción para desktop; Android lo oculta y no ejecuta quit desde ese callback. Los botones mantienen los eventos y estados visuales existentes: un Down touch muestra pressed y el click no requiere hover. El Back contextual de Android sigue siendo un corte posterior del port.

El tutorial gráfico también consume `MobileCanvasScalePolicy`: su Canvas raíz y los tres de página adoptan ancho en 16:10 y altura en teléfono ancho, sin modificar los scalers de autoría ni Windows. `TutorialSafeArea/TutorialContent` contiene las tres páginas y Next, con las referencias y orden originales. `SafeAreaAdapter` delimita el espacio disponible y `MobileReferenceContentFitter` centra/escala uniformemente la composición 1920×1080 dentro de él, incluyendo muescas que estrechan 16:9. El dimmer permanece fuera, cubriendo el frame. El ajuste geométrico no genera UI ni reactiva el tutorial jugable aislado.

El `SafeAreaAdapter` de HUD aplica la misma política al Canvas propietario en móvil. Los scalers de autoría siguen en match por altura y no se reescriben prefabs/escenas; en runtime 16:10 el HUD cambia a ancho para contener gadgets y score dentro del área segura, y vuelve a altura en formatos de teléfono. Las otras capas y Windows conservan su configuración. La matriz visual simula esta selección, sin atribuir a sus PNG una run ni el estado dinámico de puntuación.

Las páginas de Cómo Jugar pueden definir `mobileDescriptionOverrides` junto a sus descripciones: una entrada vacía conserva el texto original. MainMenu aplica esas variantes en móvil y mantiene la copia de autoría en desktop. Las instrucciones móviles indican arrastrar en pantalla y tocar `INK-PULSO`, usando el nombre visible del control. Se conservan las ilustraciones originales; la selección de texto no cambia la geometría ni crea otras páginas.

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
- La política `OptionsResolutionPolicy.SupportsResolutionSelection` también gobierna la presentación: en Android/iOS se desactivan los GameObjects completos de resolución y fullscreen, incluidos sus gráficos y etiquetas; volumen y regreso permanecen. Se aplica durante la normalización inicial y al abrir el panel. En Windows se conserva la UI desktop y las mismas claves de preferencias. Ocultar controles no modifica su valor, posición ni referencias serializadas.

En móvil, `OptionsSafeArea/OptionsContent` protege resolución/fullscreen, volumen y regreso dentro del prefab compartido. Reutiliza `SafeAreaAdapter` y `MobileReferenceContentFitter`, con composición 1920×1080; los controles no aplicables siguen ocultos. El dimmer queda como primer hijo de OptionsPanel, fuera del contenido seguro: cubre todo el viewport y bloquea raycasts al menú inferior, incluidas muescas/márgenes.

MainMenu aloja este Canvas detrás de un Transform normal. Al abrir opciones en móvil y mientras permanece abierto, el manager ajusta ese Canvas anidado al tamaño y centro del Canvas raíz; sólo escribe si cambia. Los Canvas independientes conservan el tamaño gestionado por Unity. La presentación desktop mantiene geometría, controles y preferencias de autoría. Las cuatro escenas consumen el mismo prefab; no hay Canvas, raycaster ni EventSystem adicional.

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
- `P` o `Esc` alimentan `Gameplay/TogglePause`; `PauseMenuManager` consume una solicitud por frame y conserva `TogglePause()`/`PauseMenuCommandPolicy` como autoridad.
- La suscripción se reemplaza mediante `GameplayChanged` y se dispone con el lector exacto, evitando comandos de otra escena o de una recreación anterior del scope.

Contrato de eventos:
- Los `OnClick` persistentes configurados en Inspector no deben apagarse en runtime.
- El cableado runtime de `PauseMenuManager` es respaldo defensivo y no reemplaza la auditoria visual/serializada del Inspector.

Pausa usa `PauseSafeArea/PauseContent` en el prefab compartido. Botones y decoración viven bajo ese contenido; `FondoBurbuja` permanece fuera para cubrir y bloquear el viewport completo. El adaptador y el ajuste uniforme de referencia 1920×1080 se comparten con las demás pantallas móviles. Desktop conserva stretch de autoría; no se agregan Canvas ni EventSystem, y las referencias de elementos animados conservan sus posiciones locales.

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar estado de derrota.
- Cubrir toda el area visible del canvas.
- Ofrecer navegación hacia reinicio o menú principal.
- Esperar el comic de derrota de `LoreComicPresenter.PlayDefeatIfAvailable()` antes de mostrar la vista de Game Over cuando exista una entrada valida.

El overlay oscuro debe comportarse como pantalla completa, no como subventana.
`Panel` conserva ese fondo completo; `GameOverSafeArea/GameOverContent` protege botones, decoración y ambos textos de puntaje. El ajuste móvil conserva la composición completa dentro del área segura sin modificar el cálculo ni el formato del score. Las dos zonas usan los mismos prefabs y el validador comprueba también sus instancias efectivas.
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
- `B` queda clasificada como atajo de producto exclusivo de Keyboard&Mouse y se recibe mediante `Gameplay/BuyShopOffer`. No se añade a Touch ni a un tap global; el botón `Comprar` es la vía móvil y comparte `BuyCurrentOffer()`.
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
|  |  |- Fondo (cobertura proporcional móvil)
|  |  `- ShopSafeArea/ShopContent
|  |     |- decoracion, detalle, Comprar, Volver y Opciones
|  |     |- ShrimpCounter (instancia del prefab)
|  |     `- ShopInteractionRoot
|  |        |- Upgrade01Boton ... Upgrade04Boton
|  |        |- Skin01Boton ... Skin04Boton
|  |        |- SkinAnteriorBoton
|  |        `- SkinSiguienteBoton
`- OptionsMenu (instancia del prefab global, root de escena)
```

- Cada control dentro de `ShopInteractionRoot` conserva el contrato `*Boton/Button/Visual/{Normal,Destacado,Presionado}`.
- El `Image` del hijo `Button` es una hitbox blanca con alpha `0`; no es arte ni debe recolorearse.
- Los hitboxes de `ShopInteractionRoot` usan el SFX generico de presion `Assets/Content/Audio/SFX/MainMenu/Splat.mp3`. Este contrato permite reemplazar el SFX de tienda sin tocar la jerarquía ni los sprites.
- La autoria visual de cada owner `*Boton` pertenece a la escena: posición, tamano, sprites, layout, escala, color y jerarquía no se modifican por código.
- `LetreroMejoras` y `LetreroAspectos` son imágenes decorativas 3:1 de 300×100 unidades, ancladas al borde derecho y con `preserveAspect`. No usan anchors stretch ni deltas negativos, por lo que conservan tamaño y margen en 16:9, 19.5:9 y 20:9.
- `ShopSafeArea` usa `SafeAreaAdapter`; `ShopContent` encaja uniformemente la composición 1920×1080 con `MobileReferenceContentFitter`. Protege catálogo, detalle, controles y contador; los wrappers conservan geometría stretch/escala 1 en desktop. No añaden Canvas ni EventSystem.
- `MobileBackgroundCover` mantiene `Fondo` fuera del área segura y cubre todo el viewport móvil conservando la proporción del sprite mediante overscan centrado. El contenido y los letreros no se estiran; Windows conserva su fondo de autoría. El componente sigue cambios de tamaño y sprite sin acumular escala.
- El nombre de producto habilita auto size sólo en móvil para que nombres como `Botin de Camarones` quepan en su cartel; descripciones, precios, estilos y textos desktop conservan su contrato.
- `EstadoCompraMovil` muestra el estado calculado por el presentador y los rechazos de compra, como `SIN CAMARONES`, debajo del botón Comprar. Vive dentro del contenido seguro, no recibe raycasts y permanece oculto en desktop; el campo `selectedItemStateText` referencia ese TMP y `mobilePurchaseFeedbackRoot` gobierna su visibilidad por plataforma.
- `ShopMenuCancelNavigation` escucha la acción Cancel del módulo UI existente sólo en móvil: cierra opciones si está abierto y, desde la tienda, vuelve a MainMenu. No crea otro action map ni activa acciones por su cuenta; los botones touch conservan sus eventos. Este comportamiento acota Back a ShopMenu y no sustituye la revisión global de ciclo de vida/Back.
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
- `ZonaEpipelagica` y `ZonaAbisopelagica` usan `GameRoot/GameUIRoot/HUD/SafeAreaRoot/InkBar`.

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

# Lore comics

## Alcance

El sistema de lore comics muestra viñetas antes o durante transiciones narrativas sin crear UI visual por código. La base funcional vive en el prefab `LoreComic`; la escena o prefab de zona debe contener una instancia `LoreComicRoot` y el arte final se ajusta manualmente en Unity.

Eventos implementados:
- Inicio de partida desde `MainMenu.Jugar`.
- Portal `ZonaEpipelagica -> ZonaAbisopelagica`.
- Portal `ZonaAbisopelagica -> ZonaEpipelagica`.
- Derrota en zona actual antes de mostrar Game Over.
- Comics de tienda in-game: primera entrada por `DealerFish`, primera salida con compra y primera salida sin compra.

Los hitos de puntaje quedan fuera del alcance de esta entrega y se mantienen como extension narrativa posible.

## Componente

Archivo: `Assets/Implementation/Code/Lore/LoreComicPresenter.cs`

Prefab canónico: `Assets/Content/Prefabs/UI/Menus/LoreComic.prefab`

`LoreComicPresenter` debe vivir en un GameObject activo de la escena. El Canvas visual puede estar oculto, pero el GameObject que contiene el componente debe permanecer activo para que las corrutinas puedan ejecutarse.

Referencias principales:
- `comicRoot`: Canvas o nodo raiz visual `Comic`.
- `canvasGroup`: controla visibilidad, raycasts e interaccion.
- `comicImage`: imagen donde se asigna el sprite de la vineta.
- `continueButton`: boton visual de continuar/play, preparado como parte de la autoria visual.
- `continueButtonRoot`: raiz opcional del boton si se quiere mostrar/ocultar un nodo visual completo.

El script no crea Canvas, botones, imagenes, textos ni jerarquias visuales en runtime. Solo activa/desactiva y asigna sprites en referencias existentes.

El prefab `LoreComic` contiene:
- `LoreComicRoot`: GameObject activo con `LoreComicPresenter`.
- `Comic`: Canvas overlay con `CanvasGroup`.
- `Dimmer`: fondo oscuro.
- `Vineta`: `Image` donde se muestra el sprite.
- `ContinuarBoton`: boton con contrato `Button` + `Visual`, cableado a `LoreComicPresenter.Continue()`.

El `RectTransform` de `Comic` debe conservar escala local `(1,1,1)`. La visibilidad se controla exclusivamente con `CanvasGroup` y activacion del nodo, nunca reduciendo la escala a cero. Como defensa runtime, `LoreComicPresenter` restaura escala colapsada en el root visual, en su Canvas propietario y en la imagen de vineta; esto protege el flujo, pero no reemplaza la correccion visual del prefab o escena.

## Catalogo local

`entries` define las viñetas disponibles:

| Campo | Uso |
| --- | --- |
| `comicEvent` | Evento narrativo: inicio, portal epi->abi, portal abi->epi, derrota o tienda in-game. |
| `zone` | Zona asociada. En derrota debe usarse la zona real; `Unknown` funciona como fallback. |
| `sprites` | Lista de sprites candidatos. En derrota se selecciona aleatoriamente entre sprites no nulos. |
| `displaySeconds` | Duracion minima usando tiempo real. |
| `waitForContinue` | Si debe esperar boton/confirmacion tras la duracion. |
| `showContinueButton` | Si el boton visual se muestra durante la espera. |

Reglas:
- Inicio de partida usa `LoreComicEvent.GameStart`.
- `ZonaEpipelagica -> ZonaAbisopelagica` usa `LoreComicEvent.PortalEpipelagicToAbyssopelagic`.
- `ZonaAbisopelagica -> ZonaEpipelagica` usa `LoreComicEvent.PortalAbyssopelagicToEpipelagic`.
- Derrota usa `LoreComicEvent.Defeat` y la zona activa.
- La primera entrada a tienda in-game desde `DealerFish` usa `LoreComicEvent.ShopInGameFirst` antes de abrir la tienda.
- La primera salida de esa tienda usa `LoreComicEvent.ShopInGameLastPurchased` si hubo compra, o `LoreComicEvent.ShopInGameLastNoPurchase` si no hubo compra.
- Los comics de portal y tienda in-game se consumen una vez por evento visto mediante `player-profile.json/lore.viewedComicEventIds`.
- `RuntimeInGameShopLoreState` sigue evitando repetir la entrada/salida de tienda dentro de una misma run, pero la decision persistente pertenece al perfil.
- Para derrota, cada zona debe tener al menos 3 sprites asignados.

Los sprites de comics viven organizados por dominio:
- `Assets/Content/Art/ComicLore/Inicio/`
- `Assets/Content/Art/ComicLore/Portales/`
- `Assets/Content/Art/ComicLore/Derrota/Epipelagica/`
- `Assets/Content/Art/ComicLore/Derrota/Abisopelagica/`
- `Assets/Content/Art/ComicLore/Tienda/`

La carpeta `ComicLore/Placeholders` no forma parte del paquete final de arte runtime.

## Integraciones

`MainMenu.Jugar` llama `LoreComicPresenter.PlayGameStartIfAvailable()` antes de cargar la escena de juego.

`ScenePortal` resuelve la escena destino antes de cargarla y llama `LoreComicPresenter.PlayPortalTransitionIfAvailable(targetScene)`.

`GameOverMenuManager` espera `LoreComicPresenter.PlayDefeatIfAvailable()` antes de mostrar el menú de Game Over.

`InGameShopManager` coordina los comics de tienda cuando la apertura viene desde `DealerFish`: muestra `ShopInGameFirst` antes de la primera apertura y, al cerrar esa primera tienda, elige entre `ShopInGameLastPurchased` y `ShopInGameLastNoPurchase`.

El comic de `Cómo Jugar` vive en `MainMenu` y se abre desde su botón propio.

Si no hay `LoreComicPresenter` activo en la escena, el flujo continua sin bloquearse. Portal y derrota tambien se omiten si no existe una entrada configurada para ese evento; esto evita mostrar un panel vacio durante gameplay.

Si un comic de portal o tienda in-game se muestra con una vineta valida, `LoreComicPresenter` marca su evento como visto en `PersistentPlayerProfile`. Las siguientes partidas lo omiten. Si no existe vineta valida, no se marca como visto.

Instalacion actual:
- `Assets/Scenes/MainMenu/MainMenu.unity`
- `Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaEpipelagica.prefab`
- `Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaAbisopelagica.prefab`

## Tiempo y pausa

Por defecto, `LoreComicPresenter` pausa `Time.timeScale` mientras el comic esta visible y espera usando `WaitForSecondsRealtime`. Esto permite usar comics durante gameplay, portal o Game Over sin depender del tiempo de juego.

Si una escena necesita que el mundo siga moviendose detras del comic, se puede desactivar `pauseTimeWhileShowing`.

## Criterio de entrega visual

El sistema queda listo cuando cada entrada activa del catalogo muestra al menos una vineta valida, el boton de continuar responde y el panel vuelve al flujo correspondiente sin bloquear gameplay, portal, tienda ni Game Over.

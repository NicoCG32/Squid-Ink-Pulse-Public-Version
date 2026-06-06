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

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar estado de derrota.
- Cubrir toda el area visible del canvas.
- Ofrecer navegacion hacia reinicio o menu principal.

El overlay oscuro debe comportarse como pantalla completa, no como subventana.

## Tienda temporal

Archivo: `Assets/Implementation/Code/UI/Shop/InGameShopManager.cs`

Responsabilidad:
- Abrir overlay interactuable al tocar `DealerFish`.
- Mostrar imagen del prefab de gadget seleccionado aleatoriamente.
- Mostrar precio y contador de expiracion.
- Permitir compra con camarones mediante `B`.
- Cerrar automaticamente al agotarse el tiempo.

Reglas:
- El canvas de tienda pertenece a la escena.
- El nodo manager vive en `UI/InGameShopManager`.
- El contador usa tiempo real cuando `pauseGameplayWhileOpen` esta activo.
- Los textos `B` y `Precio` pulsan para llamar la atencion.
- `SinSaldo` aparece solo despues de intentar comprar sin saldo.
- No hay boton de salir: el cierre ocurre por tiempo o compra.

## HUD

Archivos:
- `Assets/Implementation/Code/UI/HUD/ChargeBar.cs`
- `Assets/Implementation/Code/UI/HUD/ShrimpCounterDisplay.cs`
- `Assets/Implementation/Code/UI/HUD/GadgetInventoryHud.cs`

Responsabilidad:
- Mostrar carga del Ink-Pulse.
- Mostrar total de camarones persistidos en `ShrimpRuntimeWallet`.
- Mostrar gadgets con su icono dentro del hueco `GadgetN`.
- Mostrar tecla solo si el gadget del hueco es activo: `Q` en `Gadget1`, `W` en `Gadget2`.

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

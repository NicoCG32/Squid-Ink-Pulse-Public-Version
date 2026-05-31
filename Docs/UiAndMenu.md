# UI y menú

## Alcance

Este documento agrupa los sistemas de interfaz: menú principal, pausa, game over, HUD y animaciones vinculadas a UI.

## Menú principal

Archivo: `Assets/Implementation/Code/MainMenu/MainMenu.cs`

Responsabilidad:
- Gestionar la navegación del menú principal.
- Conectar acciones de inicio, salida o navegación a escenas.
- Cargar la escena de juego mediante ruta estable del asset, no mediante texto mojibakeado.

## Pausa

Archivos:
- `Assets/Implementation/Code/UI/Pause/PauseMenuManager.cs`
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`

Responsabilidad:
- Abrir y cerrar la pausa.
- Coordinar la animación de los botones.
- Mantener un efecto visual consistente mientras el juego está pausado.
- Reanudar el juego sólo después de terminar la animación de cierre.

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar el estado de derrota.
- Cubrir toda el área visible del canvas.
- Ofrecer navegación hacia reinicio o menú principal.

## HUD

Archivos:
- `Assets/Implementation/Code/UI/HUD/ChargeBar.cs`
- `Assets/Implementation/Code/UI/HUD/ShrimpCounterDisplay.cs`
- `Assets/Implementation/Code/UI/HUD/GadgetInventoryHud.cs`

Responsabilidad:
- Mostrar la carga del Ink-Pulse de forma clara.
- Convertir el estado de gameplay en una señal visual inmediata.
- Mostrar el total de camarones persistidos en `ShrimpRuntimeWallet`.
- Mostrar gadgets con su icono dentro del hueco `GadgetN` que ocupan.
- Mostrar tecla sólo si el gadget del hueco es activo: `W` en `Gadget1`, `Q` en `Gadget2`.

Regla visual de inventario:
- Los slots de inventario no tienen gadget fijo en escena; se llenan por orden de adquisición.
- El icono mostrado viene del pickup adquirido y queda registrado en `RuntimeGadgetInventory`.
- Los pasivos ocupan hueco visual, pero no exponen tecla.
- La posesión de gadgets es única; el HUD no muestra cantidades.

## UI decorativa

Archivos:
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`
- `Assets/Implementation/Code/UI/MenuScreenAnimation.cs`

Responsabilidad:
- Dar vida a pantallas de menú sin mezclar esa lógica con navegación o sesión.
- `MenuButtonAnimation` puede estar colocado en el botón o en un hijo visual/textual; en ambos casos anima el `Button` padre si existe.

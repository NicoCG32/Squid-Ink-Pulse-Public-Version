# UI y menú

## Alcance

Este documento agrupa los sistemas de interfaz: menú principal, pausa, game over, HUD y animaciones vinculadas a UI.

## Menú principal

Archivo: `Assets/Implementation/Code/MainMenu/MainMenu.cs`

Responsabilidad:
- Gestionar la navegación del menú principal.
- Conectar acciones de inicio, salida o navegación a escenas.

## Pausa

Archivos:
- `Assets/Implementation/Code/UI/Pause/PauseMenuManager.cs`
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`

Responsabilidad:
- Abrir y cerrar la pausa.
- Coordinar la animación de los botones.
- Mantener un efecto visual consistente mientras el juego está pausado.

## Game over

Archivo: `Assets/Implementation/Code/UI/GameOver/GameOverMenuManager.cs`

Responsabilidad:
- Presentar el estado de derrota.
- Ofrecer navegación hacia reinicio o menú principal.

## HUD

Archivo: `Assets/Implementation/Code/UI/HUD/ChargeBar.cs`

Responsabilidad:
- Mostrar la carga del Ink-Pulse de forma clara.
- Convertir el estado de gameplay en una señal visual inmediata.

## UI decorativa

Archivos:
- `Assets/Implementation/Code/UI/MenuButtonAnimation.cs`
- `Assets/Implementation/Code/UI/MenuBubbles.cs`
- `Assets/Implementation/Code/UI/MenuScreenAnimation.cs`

Responsabilidad:
- Dar vida a pantallas de menú sin mezclar esa lógica con navegación o sesión.

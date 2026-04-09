# Pipeline de Assets: Audio, Arte y Animaciones

## Runtime en Unity

- `Assets/Content/Audio/Soundtrack/`: Música final para juego.
- `Assets/Content/Audio/SFX/`: Efectos de sonido finales.
- `Assets/Content/Art/Characters/`: Sprites/modelos de personajes.
- `Assets/Content/Art/Enemies/`: Sprites/modelos de enemigos.
- `Assets/Content/Art/Environments/`: Arte de escenarios.
- `Assets/Content/Art/UI/`: Recursos visuales de interfaz.
- `Assets/Content/Animations/Characters/`: Animaciones de personajes.
- `Assets/Content/Animations/Enemies/`: Animaciones de enemigos.
- `Assets/Content/Animations/Environment/`: Animaciones de entorno.
- `Assets/Content/Animations/UI/`: Animaciones de interfaz.

## Runtime en UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/Character/`
- `Assets/Content/Animations/UI/MainMenu/Background/`
- `Assets/Content/Animations/UI/MainMenu/Buttons/`
- `Assets/Content/Art/UI/MainMenu/Character/`
- `Assets/Content/Art/UI/MainMenu/Background/`
- `Assets/Content/Art/UI/MainMenu/Buttons/`
- `Assets/Content/Audio/UI/MainMenu/Character/`
- `Assets/Content/Audio/UI/MainMenu/Background/`
- `Assets/Content/Audio/UI/MainMenu/Buttons/`
- `Assets/Implementation/Code/MainMenu/`

## Flujo recomendado para animaciones

1. Ubicar arte, audio y animaciones en la carpeta funcional correspondiente.
2. Integrar animaciones en prefabs o elementos de UI según dominio.
3. Vincular lógica de UI en `Assets/Implementation/Code/MainMenu/`.
4. Validar operación en escena runtime.
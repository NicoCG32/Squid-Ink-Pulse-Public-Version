# Estructura del proyecto

## Raíz

- `Assets/`: implementación Unity (código, escenas, prefabs, contenido runtime).
- `Packages/`: dependencias de Unity.
- `ProjectSettings/`: configuración del proyecto.
- `Docs/`: documentación técnica, de juego y convenciones.

## Assets

- `Assets/Implementation/Code/`: código C# organizado por dominio.
- `Assets/Implementation/Config/`: configuración técnica (input, render, plantillas).
- `Assets/Content/Prefabs/`: prefabs clasificados por tipo.
- `Assets/Content/Audio/`: soundtrack y SFX para runtime.
- `Assets/Content/Art/`: arte runtime clasificado por personajes, enemigos y escenarios.
- `Assets/Content/Animations/`: animaciones runtime (clips y controladores) clasificadas por dominio visual.
- `Assets/Scenes/`: escenas del juego y plantillas de escena.

## UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/`: animaciones del menú principal.
- `Assets/Content/Animations/UI/MainMenu/Character/`: animaciones de personaje.
- `Assets/Content/Animations/UI/MainMenu/Background/`: animaciones de fondo.
- `Assets/Content/Animations/UI/MainMenu/Buttons/`: animaciones de botones.
- `Assets/Content/Art/UI/MainMenu/`: arte del menú principal.
- `Assets/Content/Art/UI/MainMenu/Character/`: arte de personaje.
- `Assets/Content/Art/UI/MainMenu/Background/`: arte de fondo.
- `Assets/Content/Art/UI/MainMenu/Buttons/`: arte de botones.
- `Assets/Content/Audio/UI/MainMenu/`: audio del menú principal.
- `Assets/Content/Audio/UI/MainMenu/Character/`: audio de personaje.
- `Assets/Content/Audio/UI/MainMenu/Background/`: audio de fondo.
- `Assets/Content/Audio/UI/MainMenu/Buttons/`: audio de botones.
- `Assets/Implementation/Code/MainMenu/`: scripts del menú principal.

## Reglas de organización

- Cada dominio debe mantener su contenido separado por carpeta funcional.
- Cada carpeta de UI MainMenu debe contener solo assets de su categoría.
- El código de UI MainMenu se mantiene en `Assets/Implementation/Code/MainMenu/`.

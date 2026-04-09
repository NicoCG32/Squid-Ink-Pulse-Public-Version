# Estructura del Repositorio

## Raíz

- `Assets/`: Implementación Unity (código, escenas, prefabs, contenido runtime).
- `Packages/`: Dependencias de Unity.
- `ProjectSettings/`: Configuración del proyecto.
- `Docs/`: Documentación técnica, de juego y convenciones.

## Assets

- `Assets/Implementation/Code/`: Código C# organizado por dominio.
- `Assets/Implementation/Config/`: Configuración técnica (input, render, plantillas).
- `Assets/Content/Prefabs/`: Prefabs clasificados por tipo.
- `Assets/Content/Audio/`: Soundtrack y SFX para runtime.
- `Assets/Content/Art/`: Arte runtime clasificado por personajes, enemigos y escenarios.
- `Assets/Content/Animations/`: Animaciones runtime (clips y controladores) clasificadas por dominio visual.
- `Assets/Scenes/`: Escenas del juego y plantillas de escena.

## UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/`: Animaciones del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Character/`: Animaciones de personaje.
- `Assets/Content/Animations/UI/MainMenu/Background/`: Animaciones de fondo.
- `Assets/Content/Animations/UI/MainMenu/Buttons/`: Animaciones de botones.
- `Assets/Content/Art/UI/MainMenu/`: Arte del menu principal.
- `Assets/Content/Art/UI/MainMenu/Character/`: Arte de personaje.
- `Assets/Content/Art/UI/MainMenu/Background/`: Arte de fondo.
- `Assets/Content/Art/UI/MainMenu/Buttons/`: Arte de botones.
- `Assets/Content/Audio/UI/MainMenu/`: Audio del menu principal.
- `Assets/Content/Audio/UI/MainMenu/Character/`: Audio de personaje.
- `Assets/Content/Audio/UI/MainMenu/Background/`: Audio de fondo.
- `Assets/Content/Audio/UI/MainMenu/Buttons/`: Audio de botones.
- `Assets/Implementation/Code/MainMenu/`: Scripts del menu principal.

## Reglas de organización

- Cada dominio debe mantener su contenido separado por carpeta funcional.
- Cada carpeta de UI MainMenu debe contener solo assets de su categoría.
- El código de UI MainMenu se mantiene en `Assets/Implementation/Code/MainMenu/`.
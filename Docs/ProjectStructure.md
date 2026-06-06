# Estructura del proyecto

## Raiz

- `Assets/`: implementacion Unity.
- `Packages/`: dependencias de Unity.
- `ProjectSettings/`: configuracion del proyecto.
- `Docs/`: documentacion tecnica, de juego y convenciones.

## Assets

- `Assets/Implementation/Code/`: codigo C# organizado por dominio.
- `Assets/Implementation/Config/`: configuracion tecnica.
- `Assets/Content/Prefabs/`: prefabs runtime.
- `Assets/Content/Audio/`: soundtrack y SFX.
- `Assets/Content/Art/`: arte runtime.
- `Assets/Content/Animations/`: animaciones runtime.
- `Assets/Scenes/`: escenas del juego.

## Codigo por dominio

- `Assets/Implementation/Code/Core/`: sesion, escenas, camara y utilidades de mundo.
- `Assets/Implementation/Code/Audio/`: musica dinamica y transiciones de soundtrack.
- `Assets/Implementation/Code/Player/`: movimiento, Ink-Pulse, interacciones e inventario.
- `Assets/Implementation/Code/Spawning/`: spawner y catalogo de enemigos.
- `Assets/Implementation/Code/Enemies/`: comportamientos propios de enemigos.
- `Assets/Implementation/Code/Bosses/`: directores y comportamientos de boss.
- `Assets/Implementation/Code/UI/`: HUD, menus, tienda y animacion UI.
- `Assets/Implementation/Code/World/`: entidades de mundo como tienda y portales.
- `Assets/Implementation/Code/World/Lighting/`: iluminacion de zona y light graze visual.
- `Assets/Implementation/Code/Background/`: parallax y fondo.
- `Assets/Implementation/Code/MainMenu/`: menu principal.

## Prefabs por dominio

- `Assets/Content/Prefabs/Enemies/`: enemigos actuales y futuros.
- `Assets/Content/Prefabs/Bosses/`: SS Carnage y red.
- `Assets/Content/Prefabs/Collectibles/`: camarones y otros recogibles.
- `Assets/Content/Prefabs/Gadgets/`: gadgets comprables.
- `Assets/Content/Prefabs/Shop/`: `DealerFish`.
- `Assets/Content/Prefabs/Portals/`: `ScenePortal`.

## UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/`: animaciones del menu principal.
- `Assets/Content/Art/UI/MainMenu/`: arte del menu principal.
- `Assets/Content/Audio/UI/MainMenu/`: audio del menu principal.
- `Assets/Implementation/Code/MainMenu/`: scripts del menu principal.

## Reglas de organizacion

- Cada dominio mantiene contenido separado por carpeta funcional.
- Los prefabs no deben contener dependencias de escena que el runtime pueda resolver por contrato.
- Solo managers y controladores duenos de sistema deben exponer parametros ajustables de balance o flujo.
- Las entidades runtime deben recibir contexto desde managers/controladores o resolver infraestructura por contrato.
- Los boundaries se definen por nodos de escena, no por campos en componentes.
- Todo asset nuevo debe incluir su `.meta`.

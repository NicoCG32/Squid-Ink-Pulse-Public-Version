# Estructura del proyecto

## Raiz

- `Assets/`: implementacion Unity.
- `Packages/`: dependencias de Unity.
- `ProjectSettings/`: configuracion del proyecto.
- `Docs/`: documentacion tecnica, de juego y convenciones.

## Assets

- `Assets/Implementation/Code/`: codigo C# organizado por dominio.
- `Assets/Implementation/Config/`: configuracion tecnica.
- `Assets/Implementation/Config/Spawning/`: ubicacion recomendada para assets `ZoneSpawnProfile` por zona.
- `Assets/Implementation/Editor/`: herramientas de editor para migraciones o validaciones, fuera de runtime.
- `Assets/Implementation/Editor/GameplaySceneCoordinateNormalizer.cs`: herramienta para centrar las escenas jugables alrededor del origen.
- `Assets/Implementation/Editor/CleanupPrefabMigration.cs`: herramienta para crear y conectar el prefab canonico `CleanUp` en escenas jugables.
- `Assets/Content/Prefabs/`: prefabs runtime.
- `Assets/Content/Audio/`: soundtrack y SFX.
- `Assets/Content/Art/`: arte runtime.
- `Assets/Content/Animations/`: animaciones runtime.
- `Assets/Scenes/`: escenas del juego.

## Codigo por dominio

- `Assets/Implementation/Code/Core/`: sesion, escenas, camara y utilidades de mundo.
- `Assets/Implementation/Code/Audio/`: musica dinamica y transiciones de soundtrack.
- `Assets/Implementation/Code/Player/`: movimiento, Ink-Pulse, interacciones e inventario.
- `Assets/Implementation/Code/Player/Profile/`: perfil persistente, JSON local, skins y economia permanente.
- `Assets/Implementation/Code/Player/Visual/`: controladores visuales del jugador que observan estado sin decidir gameplay.
- `Assets/Implementation/Code/Spawning/`: spawner y catalogo de enemigos.
- `Assets/Implementation/Code/Enemies/`: comportamientos propios de enemigos.
- `Assets/Implementation/Code/Bosses/`: directores y comportamientos de boss.
- `Assets/Implementation/Code/UI/`: `GameUIRoot`, HUD, menus, tienda y animacion UI.
- `Assets/Implementation/Code/World/`: entidades de mundo como tienda y portales.
- `Assets/Implementation/Code/World/Lighting/`: iluminacion de zona y light graze visual.
- `Assets/Implementation/Code/Background/`: parallax y fondo.
- `Assets/Implementation/Code/MainMenu/`: menu principal.

La arquitectura formal esta definida en [SoftwareArchitecture.md](SoftwareArchitecture.md). En resumen: cada dominio contiene orquestadores (`...Manager`, `...Controller`, `...Director` o spawner de sistema), estado formal (`...State`), especializaciones concretas y datos/catalogos. La direccion de dependencia debe ser desde el dueno del sistema hacia sus estados y especializaciones, no al reves. Los perfiles de configuracion reutilizables, como `ZoneSpawnProfile`, viven como assets bajo `Assets/Implementation/Config/`.

## Prefabs por dominio

- `Assets/Content/Prefabs/Enemies/`: enemigos actuales y futuros.
- `Assets/Content/Prefabs/Player/`: `BabySquid.prefab`, fuente canonica del jugador.
- `Assets/Content/Prefabs/Bosses/`: SS Carnage y red.
- `Assets/Content/Prefabs/Collectibles/`: camarones y otros recogibles.
- `Assets/Content/Prefabs/Gadgets/`: gadgets comprables.
- `Assets/Content/Prefabs/Shop/`: `DealerFish`.
- `Assets/Content/Prefabs/Portals/`: `ScenePortal`.
- `Assets/Content/Prefabs/World/`: `CleanUp.prefab`, limpieza fuera de camara adaptada a `CameraBoundaries`.
- `Assets/Content/Prefabs/UI/`: vistas de HUD y menus consumidas por `GameUIRoot` en escenas jugables.

## UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/`: animaciones del menu principal.
- `Assets/Content/Art/UI/MainMenu/`: arte del menu principal.
- `Assets/Content/Audio/UI/MainMenu/`: audio del menu principal.
- `Assets/Implementation/Code/MainMenu/`: scripts del menu principal.

## Reglas de organizacion

- Cada dominio mantiene contenido separado por carpeta funcional.
- Cada script nuevo debe clasificarse como orquestador, estado, especializacion o dato antes de agregarse.
- Los prefabs no deben contener dependencias de escena que el runtime pueda resolver por contrato.
- Solo managers y controladores duenos de sistema deben exponer parametros ajustables de balance o flujo.
- Las entidades runtime deben recibir contexto desde managers/controladores o resolver infraestructura por contrato.
- Los boundaries se definen por nodos de escena, no por campos en componentes.
- Todo asset nuevo debe incluir su `.meta`.

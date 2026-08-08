# Estructura del proyecto

## Raiz

- `Assets/`: implementación Unity.
- `Packages/`: dependencias de Unity.
- `ProjectSettings/`: configuración del proyecto.
- `Docs/`: documentación técnica, de juego y convenciones.

## Assets

- `Assets/Implementation/Code/`: código C# organizado por dominio.
- `Assets/Implementation/Config/`: configuración técnica.
- `Assets/Implementation/Config/Input/`: asset versionado del Input System con mapas `Gameplay` y `UI`.
- `Assets/Implementation/Config/Spawning/`: assets `ZoneSpawnProfile` por zona (`ZonaEpipelagicaSpawnProfile`, `ZonaAbisopelagicaSpawnProfile`).
- `Assets/Implementation/Editor/`: soporte de build final. Actualmente contiene el postprocesador que genera instrucciones y scripts de reinicio para builds de feria.
- `Assets/Content/Prefabs/`: prefabs runtime.
- `Assets/Content/Audio/`: soundtrack y SFX.
- `Assets/Content/Art/`: arte runtime.
- `Assets/Content/Art/ComicLore/`: viñetas narrativas separadas por dominio (`Inicio`, `Portales`, `Derrota`, `Tienda`).
- `Assets/Content/Art/Environments/ShopMenu/Fondo.png`: fondo ambiental de la escena `ShopMenu`; los botones y decoraciones de tienda permanecen en `Assets/Content/Art/UI/ShopMenu/`.
- `Assets/Content/Animations/`: animaciones runtime.
- `Assets/Scenes/`: escenas del juego.
- `Assets/Implementation/Resources/PersistentDbSeeds/`: semillas JSON incluidas en build para catalogo, perfil, records y leaderboard local.

## Codigo por dominio

- `Assets/Implementation/Code/Core/`: sesión, escenas, cámara y utilidades de mundo.
- `Assets/Implementation/Code/Input/`: nombres y contrato semántico compartido de acciones; el lector runtime se incorpora de forma incremental.
- `Assets/Implementation/Code/Audio/`: musica dinamica, crossfade de Ink-Pulse, progresión de pitch del soundtrack y volumen maestro global.
- `Assets/Implementation/Code/Player/`: movimiento, Ink-Pulse, interacciones e inventario.
- `Assets/Implementation/Code/Player/Profile/`: persistencia JSON local, catalogo de desbloqueables, perfil, records, skins, economia permanente y leaderboard local.
- `Assets/Implementation/Code/Player/Visual/`: controladores visuales del jugador que observan estado sin decidir gameplay.
- `Assets/Implementation/Code/Fair/`: adaptador Unity para el add-on opcional de feria LAN. El alcance documentado es leaderboard en host visible desde navegador; no reemplaza la persistencia local del juego.
- `Assets/Implementation/Code/Spawning/`: `LevelSpawner`, perfiles de spawn y servicios internos de seleccion, posición y configuración de entidades spawneadas.
- `Assets/Implementation/Code/Enemies/`: comportamientos propios de enemigos.
- `Assets/Implementation/Code/Bosses/`: directores y comportamientos de boss.
- `Assets/Implementation/Code/UI/`: `GameUIRoot`, HUD, menus, tienda y animación UI.
- `Assets/Implementation/Code/UI/Shop/OutOfGameShopManager.cs`: coordinador de la tienda permanente de `ShopMenu`.
- `Assets/Implementation/Code/Lore/`: presentación narrativa por viñetas y seleccion de comics.
- `Assets/Implementation/Code/World/`: entidades de mundo como tienda y portales.
- `Assets/Implementation/Code/World/Lighting/`: iluminacion de zona y light graze visual.
- `Assets/Implementation/Code/Background/`: parallax y fondo.
- `Assets/Implementation/Code/MainMenu/`: menú principal.

La arquitectura formal esta definida en [SoftwareArchitecture.md](SoftwareArchitecture.md). En resumen: cada dominio contiene orquestadores (`...Manager`, `...Controller`, `...Director` o spawner de sistema), estado formal (`...State`), especializaciones concretas, servicios internos (`...Selector`, `...Resolver`, `...Configurator`, `...Calculator`) y datos/catalogos. La direccion de dependencia debe ser desde el dueno del sistema hacia sus estados, servicios y especializaciones, no al reves. Los perfiles de configuración reutilizables, como `ZoneSpawnProfile`, viven como assets bajo `Assets/Implementation/Config/`.

## Prefabs por dominio

- `Assets/Content/Prefabs/Enemies/`: enemigos de entrega y extensiones preparadas.
- `Assets/Content/Prefabs/Player/`: `BabySquid.prefab`, fuente canonica del jugador.
- `Assets/Content/Prefabs/Bosses/`: SS Carnage y red.
- `Assets/Content/Prefabs/Collectibles/`: camarones y otros recogibles.
- `Assets/Content/Prefabs/Gadgets/`: gadgets comprables durante la run mediante `DealerFish`.
- `Assets/Content/Prefabs/Shop/`: `DealerFish`.
- `Assets/Content/Prefabs/Portals/`: `ScenePortal`.
- `Assets/Content/Prefabs/Core/Audio/`: `AudioRoot_*` por zona, con `Soundtrack`, `SFX` y componentes de musica dinamica por zona.
- `Assets/Content/Prefabs/Core/Camera/`: `CameraRig_*` por zona, con `Main Camera` y `CameraController`.
- `Assets/Content/Prefabs/Core/Environment/`: `EnviromentRoot_*` por zona, con fondos, parallax y efectos ambientales.
- `Assets/Content/Prefabs/Core/Scenes/`: `GameRoot_*` por zona, como raiz de composicion jugable.
- `Assets/Content/Prefabs/World/`: `Boundaries.prefab` como contrato fisico de límites y `CleanUp.prefab` como limpieza fuera de cámara adaptada a `CameraBoundaries`.
- `Assets/Content/Prefabs/UI/`: vistas de HUD, menus y overlays narrativos consumidas por escenas o `GameUIRoot`.

## UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/`: animaciones del menú principal.
- `Assets/Content/Art/UI/MainMenu/`: arte del menú principal.
- `Assets/Content/Audio/UI/MainMenu/`: audio del menú principal.
- `Assets/Implementation/Code/MainMenu/`: scripts del menú principal.

## Reglas de organizacion

- Cada dominio mantiene contenido separado por carpeta funcional.
- Cada script nuevo debe clasificarse como orquestador, estado, especializacion, servicio interno o dato antes de agregarse.
- Los prefabs no deben contener dependencias de escena que el runtime pueda resolver por contrato.
- Los prefabs `AudioRoot_*`, `CameraRig_*`, `EnviromentRoot_*` y `GameRoot_*` son prefabs de composicion por zona. Pueden conservar overrides de escena, pero no deben transformarse en fuente de balance global que pertenezca a managers o profiles.
- Solo managers y controladores dueños de sistema deben exponer parámetros ajustables de balance o flujo.
- Las entidades runtime deben recibir contexto desde managers/controladores o resolver infraestructura por contrato.
- Los boundaries se definen por nodos de escena, no por campos en componentes.
- Todo asset nuevo debe incluir su `.meta`.
- Los JSON persistentes separan la fuente empaquetada en `Assets/Implementation/Resources/PersistentDbSeeds` del estado runtime en `Application.persistentDataPath/db`.
- Si una regla puede expresarse como cálculo puro, selector o configurador, no debe vivir dentro de un `MonoBehaviour` por conveniencia.

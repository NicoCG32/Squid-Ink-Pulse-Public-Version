# Estructura del proyecto

## Raiz

- `Assets/`: implementacion Unity.
- `Packages/`: dependencias de Unity.
- `ProjectSettings/`: configuracion del proyecto.
- `Docs/`: documentacion tecnica, de juego y convenciones.

## Assets

- `Assets/Implementation/Code/`: codigo C# organizado por dominio.
- `Assets/Implementation/Config/`: configuracion tecnica.
- `Assets/Implementation/Config/Spawning/`: assets `ZoneSpawnProfile` por zona (`ZonaEpipelagicaSpawnProfile`, `ZonaAbisopelagicaSpawnProfile`, `ZonaTutorialSpawnProfile`).
- `Assets/Implementation/Editor/`: herramientas de editor para migraciones o validaciones, fuera de runtime.
- `Assets/Implementation/Editor/GameplaySceneCoordinateNormalizer.cs`: herramienta para centrar las escenas jugables alrededor del origen.
- `CODEX/`: memoria operativa para continuar trabajo con Codex; no reemplaza la documentacion tecnica de `Docs/`.
- `Assets/Implementation/Editor/CleanupPrefabMigration.cs`: herramienta para crear y conectar el prefab canonico `CleanUp` en escenas jugables.
- `Assets/Implementation/Editor/SceneContractValidator.cs`: herramienta `Tools/Squid/Validate Scene Contracts` para auditar zonas jugables, prefabs criticos, perfiles de spawn, tags y layers.
- `Assets/Content/Prefabs/`: prefabs runtime.
- `Assets/Content/Audio/`: soundtrack y SFX.
- `Assets/Content/Art/`: arte runtime.
- `Assets/Content/Animations/`: animaciones runtime.
- `Assets/Scenes/`: escenas del juego.
- `Assets/StreamingAssets/db/`: semillas JSON incluidas en build para catalogo, perfil, records y leaderboard local.

## Codigo por dominio

- `Assets/Implementation/Code/Core/`: sesion, escenas, camara y utilidades de mundo.
- `Assets/Implementation/Code/Audio/`: musica dinamica y transiciones de soundtrack.
- `Assets/Implementation/Code/Player/`: movimiento, Ink-Pulse, interacciones e inventario.
- `Assets/Implementation/Code/Player/Profile/`: persistencia JSON local, catalogo de desbloqueables, perfil, records, skins, economia permanente y leaderboard local.
- `Assets/Implementation/Code/Player/Visual/`: controladores visuales del jugador que observan estado sin decidir gameplay.
- `Assets/Implementation/Code/Spawning/`: `LevelSpawner`, perfiles de spawn y servicios internos de seleccion, posicion y configuracion de entidades spawneadas.
- `Assets/Implementation/Code/Enemies/`: comportamientos propios de enemigos.
- `Assets/Implementation/Code/Bosses/`: directores y comportamientos de boss.
- `Assets/Implementation/Code/UI/`: `GameUIRoot`, HUD, menus, tienda y animacion UI.
- `Assets/Implementation/Code/Tutorial/`: director y pasos formales del tutorial.
- `Assets/Implementation/Code/World/`: entidades de mundo como tienda y portales.
- `Assets/Implementation/Code/World/Lighting/`: iluminacion de zona y light graze visual.
- `Assets/Implementation/Code/Background/`: parallax y fondo.
- `Assets/Implementation/Code/MainMenu/`: menu principal.

La arquitectura formal esta definida en [SoftwareArchitecture.md](SoftwareArchitecture.md). En resumen: cada dominio contiene orquestadores (`...Manager`, `...Controller`, `...Director` o spawner de sistema), estado formal (`...State`), especializaciones concretas, servicios internos (`...Selector`, `...Resolver`, `...Configurator`, `...Calculator`) y datos/catalogos. La direccion de dependencia debe ser desde el dueno del sistema hacia sus estados, servicios y especializaciones, no al reves. Los perfiles de configuracion reutilizables, como `ZoneSpawnProfile`, viven como assets bajo `Assets/Implementation/Config/`.

## Prefabs por dominio

- `Assets/Content/Prefabs/Enemies/`: enemigos actuales y futuros.
- `Assets/Content/Prefabs/Player/`: `BabySquid.prefab`, fuente canonica del jugador.
- `Assets/Content/Prefabs/Bosses/`: SS Carnage y red.
- `Assets/Content/Prefabs/Collectibles/`: camarones y otros recogibles.
- `Assets/Content/Prefabs/Gadgets/`: gadgets comprables durante la run mediante `DealerFish`.
- `Assets/Content/Prefabs/Shop/`: `DealerFish`.
- `Assets/Content/Prefabs/Portals/`: `ScenePortal`.
- `Assets/Content/Prefabs/Core/Audio/`: `AudioRoot_*` por zona, con `Soundtrack` y `SFX`.
- `Assets/Content/Prefabs/Core/Camera/`: `CameraRig_*` por zona, con `Main Camera` y `CameraController`.
- `Assets/Content/Prefabs/Core/Environment/`: `EnviromentRoot_*` por zona, con fondos, parallax y efectos ambientales.
- `Assets/Content/Prefabs/Core/Scenes/`: `GameRoot_*` por zona, como raiz de composicion jugable.
- `Assets/Content/Prefabs/World/`: `Boundaries.prefab` como contrato fisico de limites y `CleanUp.prefab` como limpieza fuera de camara adaptada a `CameraBoundaries`.
- `Assets/Content/Prefabs/UI/`: vistas de HUD y menus consumidas por `GameUIRoot` en escenas jugables.

## UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/`: animaciones del menu principal.
- `Assets/Content/Art/UI/MainMenu/`: arte del menu principal.
- `Assets/Content/Audio/UI/MainMenu/`: audio del menu principal.
- `Assets/Implementation/Code/MainMenu/`: scripts del menu principal.

## Reglas de organizacion

- Cada dominio mantiene contenido separado por carpeta funcional.
- Cada script nuevo debe clasificarse como orquestador, estado, especializacion, servicio interno o dato antes de agregarse.
- Los prefabs no deben contener dependencias de escena que el runtime pueda resolver por contrato.
- Los prefabs `AudioRoot_*`, `CameraRig_*`, `EnviromentRoot_*` y `GameRoot_*` son prefabs de composicion por zona. Pueden conservar overrides de escena, pero no deben transformarse en fuente de balance global que pertenezca a managers o profiles.
- Solo managers y controladores duenos de sistema deben exponer parametros ajustables de balance o flujo.
- Las entidades runtime deben recibir contexto desde managers/controladores o resolver infraestructura por contrato.
- Los boundaries se definen por nodos de escena, no por campos en componentes.
- Todo asset nuevo debe incluir su `.meta`.
- Los JSON persistentes deben organizarse bajo `db`: semillas en `Assets/StreamingAssets/db` y runtime en `Application.persistentDataPath/db`.
- Si una regla puede expresarse como calculo puro, selector o configurador, no debe vivir dentro de un `MonoBehaviour` por conveniencia.

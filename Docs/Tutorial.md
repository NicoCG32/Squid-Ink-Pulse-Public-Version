# Tutorial

## Alcance activo

El tutorial disponible para jugadores es el comic de `Como Jugar` del `MainMenu`.

Contrato activo:

- `ComoJugarBoton` abre `MainMenu.AbrirTutorial()`.
- `MainMenu.AbrirTutorial()` muestra las paginas configuradas bajo `ComicsTutorial`.
- `Next` llama `MainMenu.AvanzarTutorial()` y avanza o cierra el comic.
- Este flujo no carga una escena jugable adicional.

## Tutorial jugable pendiente

`Assets/Scenes/Game/ZonaTutorial.unity` es una implementacion futura aislada. Existe en el proyecto junto con `TutorialDirector`, sus controladores de HUD/presentacion, su perfil de spawn y prefabs especificos, pero no forma parte del producto activo.

Activos asociados al tutorial pendiente:

- `Assets/Scenes/Game/ZonaTutorial.unity`
- `Assets/Implementation/Code/Tutorial/`
- `Assets/Implementation/Config/Spawning/ZonaTutorialSpawnProfile.asset`
- `Assets/Content/Prefabs/Core/Scenes/GameRoot_ZonaTutorial.prefab`
- `Assets/Content/Prefabs/Core/Camera/CameraRig_ZonaTutorial.prefab`
- `Assets/Content/Prefabs/Core/Audio/AudioRoot_ZonaTutorial.prefab`
- `Assets/Content/Prefabs/Core/Environment/EnviromentRoot_ZonaTutorial.prefab`

Estos assets pueden conservar referencias entre si, pero no deben conectarse a rutas de usuario mientras el tutorial jugable siga pendiente.

## Build Settings

El build activo contiene solo estas escenas:

1. `Assets/Scenes/MainMenu/MainMenu.unity`
2. `Assets/Scenes/Game/ZonaEpipelagica.unity`
3. `Assets/Scenes/Game/ZonaAbisopelagica.unity`
4. `Assets/Scenes/ShopMenu/ShopMenu.unity`

`Assets/Scenes/Game/ZonaTutorial.unity` debe permanecer fuera de `EditorBuildSettings`.

`Assets/Implementation/Editor/TutorialBuildSettingsValidator.cs` implementa una validacion de editor y prebuild. Si `ZonaTutorial.unity` queda habilitada accidentalmente en Build Settings, la validacion falla antes de compilar.

La validacion tambien puede ejecutarse manualmente desde:

```text
Tools/Squid Ink Pulse/Validate Tutorial Isolation
```

## SceneFlowController

`SceneFlowController.LoadTutorial()` se conserva como API pendiente para no romper referencias serializadas futuras o experimentales. No debe existir una ruta de usuario activa que llame a ese metodo mientras `ZonaTutorial` este fuera del build.

Si en una tarea futura se decide implementar el tutorial jugable, esa tarea debe completar y probar `TutorialDirector`, habilitar formalmente la escena, actualizar este documento y redefinir la validacion de build.

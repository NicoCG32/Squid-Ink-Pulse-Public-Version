# Sistemas nucleo

## Alcance

Este documento cubre la capa de orquestacion: sesion global, progresion de run, flujo de escenas y reglas base que afectan a todos los sistemas.

## GameSessionController

Archivo: `Assets/Implementation/Code/Core/Session/GameSessionController.cs`

Responsabilidad:
- Controlar el estado global del juego.
- Aplicar `Time.timeScale` segun el estado.
- Exponer eventos para que otros sistemas respondan sin acoplarse directamente.
- Reiniciar `RuntimeGadgetInventory` y `RuntimeInkPulseState` al entrar en `GameOver`.

Estados:
- `Playing`
- `Paused`
- `GameOver`

Regla de persistencia:
- Cruzar un portal no cambia a `GameOver`; por tanto conserva gadgets e Ink-Pulse.
- Entrar en `GameOver` limpia gadgets e Ink-Pulse para la siguiente partida.
- La billetera de camarones persiste fuera del juego mediante `PersistentPlayerProfile`.
- Al entrar en `GameOver`, `PersistentPlayerProfile` registra `bestScore` y `totalRuns` antes de limpiar score runtime.

## PersistentPlayerProfile

Archivos:
- `Assets/Implementation/Code/Player/Profile/PlayerProfileSaveData.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerProfileRepository.cs`
- `Assets/Implementation/Code/Player/Profile/PersistentPlayerProfile.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerSkinIds.cs`

Responsabilidad:
- Cargar y guardar `player-profile.json` en `Application.persistentDataPath`.
- Persistir saldo de camarones, upgrades permanentes, skins y stats.
- Normalizar defaults, incluyendo la skin base `skin.default`.
- Mantener settings fuera de este archivo.

La especificacion completa esta en [PersistentProfile.md](PersistentProfile.md).

## RunProgressionDirector

Archivos:
- `Assets/Implementation/Code/Core/Session/RunProgressionDirector.cs`
- `Assets/Implementation/Code/Core/Session/RunEventState.cs`
- `Assets/Implementation/Code/Core/Session/RunDifficultySnapshot.cs`

Responsabilidad:
- Llevar el ritmo de la run.
- Calcular intensidad, scroll y spawn.
- Gestionar ventanas de boss y transicion.
- Modular la frecuencia de spawn segun estado macro.
- Separar el reloj de intensidad del reloj de reaparicion de boss, para sostener presion alta sin disparar otro SS Carnage de inmediato.

Estados de evento:
- `Normal`
- `BossActive`
- `PostBossWindow`
- `Transitioning`

Reglas de spawn por evento:
- `Normal`: usa el intervalo base calculado por intensidad.
- `BossActive`: reduce el intervalo con `bossActiveSpawnIntervalMultiplier`; por defecto `0.5`, equivalente a doble frecuencia.
- `PostBossWindow`: conserva la intensidad alcanzada tras el boss mientras ofrece la oportunidad de portal.
- `Transitioning`: bloquea spawn regular; al completar portal, la zona destino empieza relajada.

## SceneFlowController

Archivo: `Assets/Implementation/Code/Core/Scenes/SceneFlowController.cs`

Responsabilidad:
- Cargar escenas por nombre, indice o ruta `.unity`.
- Reiniciar la escena actual.
- Reiniciar una run desde `primaryGameplaySceneName`.
- Volver al menu principal.
- Restaurar `Time.timeScale` antes de cambiar de escena.
- Preparar rutas conocidas para `ZonaTutorial`, `ShopMenu` y `OptionsMenu`.

Uso por portales:
- `ScenePortal` usa `SceneFlowController` como fuente obligatoria de destino.
- `primaryGameplaySceneName` y `secondaryGameplaySceneName` definen el par de zonas jugables: `ZonaEpipelagica` y `ZonaAbisopelagica`.
- Las escenas destino deben estar registradas en Build Settings.
- La carga por portal no reinicia gadgets ni Ink-Pulse.
- La carga por portal tampoco reinicia score ni pace runtime.

Uso por Game Over:
- `GameOverMenuManager.Retry()` no recarga la escena activa.
- Reintentar siempre inicia una run nueva desde `primaryGameplaySceneName`, aunque la derrota haya ocurrido en `ZonaAbisopelagica`.
- Ese reintento limpia estado de run: gadgets, Ink-Pulse, score y pace.

## BoundaryReferenceResolver

Archivo: `Assets/Implementation/Code/Core/World/BoundaryReferenceResolver.cs`

Aunque vive en `Core/World`, es infraestructura transversal:
- Define el contrato formal de `PlayerBoundaries` y `CameraBoundaries`.
- Evita que cada sistema guarde referencias manuales a limites.
- Permite que zonas nuevas sean compatibles si respetan la misma jerarquia.

La especificacion completa esta en [WorldAndCamera.md](WorldAndCamera.md).

## Reglas compartidas

- La sesion global manda sobre pausa, game over y reanudacion.
- La progresion no debe mezclarse con logica de UI.
- Los cambios de zona no deben limpiar estado runtime salvo que la sesion entre en Game Over o se ejecute un reintento explicito.
- Los cambios de zona se disparan por `ScenePortal`, pero las rutas pertenecen a `SceneFlowController`.
- Los limites de escena pertenecen a `PlayerBoundaries` y `CameraBoundaries`, no a campos manuales de scripts.
- La limpieza fuera de pantalla pertenece a `DestroyOffscreen`; su posicion runtime se deriva de la camara, no de coordenadas manuales.
- Las escenas jugables se componen alrededor del origen: `Main Camera` inicia en `(0, 0, -10)` y `Squid` en `(-5, 0, 0)`.
- No se corrige tamano escalando roots estructurales; un escalado global requiere balancear tambien velocidad, camara, spawn, colliders y offsets.

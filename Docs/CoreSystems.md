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
- La billetera de camarones persiste durante runtime; su almacenamiento permanente sigue pendiente.

## RunProgressionDirector

Archivo: `Assets/Implementation/Code/Core/Session/RunProgressionDirector.cs`

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
- Volver al menu principal.
- Restaurar `Time.timeScale` antes de cambiar de escena.
- Preparar rutas conocidas para `ZonaTutorial`, `ShopMenu` y `OptionsMenu`.

Uso por portales:
- `ScenePortal` usa `SceneFlowController` como fuente obligatoria de destino.
- `primaryGameplaySceneName` y `secondaryGameplaySceneName` definen el par de zonas jugables: `ZonaEpipelagica` y `ZonaAbisopelagica`.
- Las escenas destino deben estar registradas en Build Settings.
- La carga por portal no reinicia gadgets ni Ink-Pulse.

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
- Los cambios de escena no deben limpiar estado runtime salvo que la sesion entre en Game Over.
- Los cambios de zona se disparan por `ScenePortal`, pero las rutas pertenecen a `SceneFlowController`.
- Los limites de escena pertenecen a `PlayerBoundaries` y `CameraBoundaries`, no a campos manuales de scripts.
- La limpieza fuera de pantalla pertenece a `DestroyOffscreen`; su posicion runtime se deriva de la camara, no de coordenadas manuales.

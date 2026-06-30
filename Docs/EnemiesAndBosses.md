# Enemigos y jefes

## Alcance

Este documento reune el sistema de spawn, el catalogo de enemigos, los enemigos actuales, SS Carnage y el boss abisal `UnknownBoss` / `FlappyBoss`.

## LevelSpawner

Archivo: `Assets/Implementation/Code/Spawning/LevelSpawner.cs`

Responsabilidad:
- Generar enemigos, camarones, tienda y portales segun la progresion de la run.
- Ajustar intervalos y distribucion vertical segun intensidad y boundaries.
- Respetar la frecuencia de spawn que entrega `RunProgressionDirector`.
- Suspender spawn regular solo cuando `RunEventState` esta en `Transitioning`.
- Instanciar enemigos unicamente desde `enemyProfiles`.

Cada perfil define prefab, tag logico, peso de aparicion, intensidad minima y multiplicador de intervalo. `LevelSpawner` conserva la autoridad de instanciar, pero delega trabajo interno:
- `EnemySpawnSelector`: seleccion de perfil por intensidad, pesos y regla de cana forzada.
- `SpawnPositionResolver`: calculo de posiciones desde camara, boundaries, jugador y `ZoneSpawnProfile`.
- `SpawnedObjectConfigurator`: aplicacion de tag, layer, `LightGrazeSource` y `EnemySpawnContext`.

Durante `BossActive`, el spawner no se detiene: recibe un intervalo reducido desde la progresion, por lo que los obstaculos aparecen con mayor frecuencia. Durante `PostBossWindow`, la run conserva intensidad alta mientras ofrece el portal. Si el jugador no cruza, la partida sigue intensa; si cruza, la zona destino empieza relajada.

La excepcion deliberada es `EnemyCanaPescar`: la caña regular pertenece al modo normal de spawner. Durante `BossActive` no se fuerza desde `LevelSpawner`, porque el anzuelo de Carnage debe modelarse como ataque propio del boss, con prefab y controlador especificos.

## Distribucion vertical

El spawn vertical depende del contrato de boundaries:
- Camarones usan el rango visible de `CameraBoundaries`, intersectado con la camara y con `PlayerBoundaries`, por lo que no aparecen sobre el `TopBoundary` del jugador.
- Enemigos, `DealerFish` y portales usan `PlayerBoundaries`.
- Pez Globo aparece en los tres cuartos superiores de la mitad superior.
- Mina aparece en los tres cuartos inferiores de la mitad inferior.
- Cana de pescar aparece por la derecha, cada `fishingRodEnemyInterval` enemigos en juego normal. Captura la altura del jugador al spawnear, calcula una distancia X proporcional a la velocidad horizontal actual del jugador, nace arriba y baja verticalmente hasta esa Y fija.

No hay rangos manuales de respaldo para spawn.

## EnemyTagCatalog

Archivo: `Assets/Implementation/Code/Spawning/EnemyTagCatalog.cs`

Responsabilidad:
- Centralizar tags logicos de enemigos.
- Evitar strings duplicados en colision, graze, limpieza o spawn.

Tags actuales:
- `Enemy`
- `EnemyMina`
- `EnemyPezGlobo`
- `EnemyCanaPescar`

## EnemySpawnContext

Archivos:
- `Assets/Implementation/Code/Enemies/EnemySpawnContext.cs`
- `Assets/Implementation/Code/Enemies/IEnemySpawnContextReceiver.cs`

Responsabilidad:
- Recibir contexto minimo de spawn cuando un enemigo necesita camara o jugador.

Contrato actual:
- Recibe `Camera` y `Transform player`.
- Recibe tuning de comportamiento especifico desde `LevelSpawner` mediante `SpawnedObjectConfigurator` cuando aplica.
- No recibe boundaries.
- Si el enemigo necesita limites, debe resolverlos con `BoundaryReferenceResolver`.

## Enemigos actuales

### PufferfishEnemy

Archivo: `Assets/Implementation/Code/Enemies/PufferfishEnemy.cs`

Responsabilidad:
- Moverse verticalmente con direccion aleatoria.
- Expandirse una sola vez cuando el jugador entra en `proximityRadius`.
- Aumentar velocidad durante expansion sin forzar subida.
- Reproducir la animacion de hinchado una sola vez al comenzar la expansion.
- Permanecer expandido; no vuelve a deshincharse aunque el jugador se aleje.
- No sobrepasar el `TopBoundary` de `PlayerBoundaries`.
- Usar `CircleCollider2D` como collider corporal unico; al escalar el enemigo, el collider acompana la expansion.

Parametros de balance:
- `fallSpeed`
- `expandedSpeedMultiplier`
- `proximityRadius`
- `expandedScaleMultiplier`
- `expansionSmoothSpeed`
- `erraticDirectionChangeIntervalMin`
- `erraticDirectionChangeIntervalMax`
- `erraticDirectionChangeChance`

Estos parametros pertenecen a `ZoneSpawnProfile.pufferfishTuning`. El prefab `PezGlobo` no debe exponerlos: su script solo ejecuta comportamiento con el contexto recibido al spawnear.

### Mina

Estado actual:
- Prefab y tag implementados.
- Sin script propio todavia.
- Su comportamiento actual es estatico y su aparicion depende de `LevelSpawner`.

### Cana de pescar

Estado actual:
- Prefab y tag implementados.
- Script propio `FishingRodEnemy` implementado.
- En juego normal se fuerza cada `fishingRodEnemyInterval` enemigos.
- Aparece desde la derecha, captura la Y del jugador en el momento de spawn y queda arriba hasta entrar en su ventana de lectura.
- Al llegar a la ventana de lectura, espera una pausa breve configurable y baja desde el top del rango jugable hasta la Y capturada.
- No persigue al jugador despues de capturar la Y.
- Durante `BossActive`, el spawner regular no fuerza cañas; los anzuelos del SS Carnage deben implementarse como ataque de boss separado.

Parametros de balance:
- `dropSpeed`
- `startYOffsetBelowTopBoundary`
- `descentStartViewportX`
- `descentWindupSeconds`
- `enableFastPaceHorizontalHold`
- `horizontalHoldMinScrollSpeed`
- `horizontalHoldViewportX`
- `arriveDistance`
- `horizontalLeadTimePaddingSeconds`
- `minimumHorizontalLeadDistance`

Estos parametros pertenecen a `ZoneSpawnProfile.fishingRodTuning`.

Contrato de legibilidad:
- La bajada no debe consumirse completamente fuera de camara.
- `descentStartViewportX` define en que posicion horizontal de viewport se permite iniciar la accion: `1` es el borde derecho de camara y valores mayores empiezan levemente antes de entrar.
- `descentWindupSeconds` agrega una pausa corta antes de caer, sin recapturar la posicion del jugador.
- `enableFastPaceHorizontalHold` corrige la X de la cana cuando la velocidad horizontal supera `horizontalHoldMinScrollSpeed`, manteniendola cerca de `horizontalHoldViewportX` mientras espera, anticipa y baja.
- El anclaje rapido se libera al llegar a la Y capturada; desde ese punto la cana vuelve a comportarse como obstaculo normal del mundo.
- El anclaje rapido no recaptura la Y del jugador y no debe convertirse en persecucion vertical.
- `SpawnPositionResolver` incluye la duracion de bajada, la pausa y el margen horizontal al calcular la distancia de spawn, para que mejorar la lectura no vuelva injusta la amenaza.

Contrato visual de `CanaPescar.prefab`:
- El largo de `Rope` y la escala de `Visual` son autoria del prefab. No deben normalizarse por codigo ni desde `ZoneSpawnProfile`.
- El root mantiene la identidad jugable: tag `EnemyCanaPescar`, layer `Enemy` y script `FishingRodEnemy`.
- `Rope` y `Visual` son hijos visuales/estructurales del enemigo; deben permanecer en layer `Enemy` y sin tag logico propio salvo que una mecanica futura lo justifique.
- Si la cana se hace mas grande visualmente, el cleanup debe respetar sus bounds agregados. Esto implica destruirla mas tarde, cuando todo el volumen visual/fisico ya quedo detras de la distancia segura.
- Si el cambio de tamano hace que la amenaza se lea demasiado pronto, demasiado tarde o demasiado injusta, el ajuste correcto es `ZoneSpawnProfile.fishingRodTuning`, no una correccion de escala en runtime.

## BossEventDirector

Archivo: `Assets/Implementation/Code/Bosses/BossEventDirector.cs`

Responsabilidad:
- Coordinar el arranque de eventos de boss.
- Solicitar vista amplia al `CameraController`.
- Instanciar el prefab de boss por la derecha de la camara.
- Entregar contexto de sesion, progresion, camara y parent al boss activo.

No entrega boundaries al boss. La escena debe proveer `CameraBoundaries` y `PlayerBoundaries`, y cada consumidor los resuelve por dominio.

Uso por zona:
- `ZonaEpipelagica` usa `BossEventDirector` para instanciar `SSCarnage`.
- `ZonaAbisopelagica` usa `BossEventDirector` en el nodo `FlappyBossManager` para instanciar `UnknownBoss`.
- `ZonaTutorial` puede tener director de boss si el `TutorialDirector` lo habilita como parte de su flujo.

## UnknownBoss / FlappyBoss

Archivos:
- `Assets/Implementation/Code/Bosses/UnknownBoss/FlappyBossController.cs`
- `Assets/Content/Prefabs/Bosses/UnknownBoss/UnknownBoss.prefab`
- `Assets/Content/Prefabs/Bosses/UnknownBoss/BossPillars.prefab`

Responsabilidad:
- Controlar el boss propio de `ZonaAbisopelagica`.
- Usar pilares tipo Flappy Bird como amenaza principal.
- Resolver alturas desde `CameraBoundaries`, no desde constantes fijas.
- Avisar a `RunProgressionDirector` con `NotifyBossResolved()` al terminar su secuencia.

Contrato:
- `UnknownBoss` usa tag `Boss` y layer `Boss`.
- `BossPillars` usan layer `Enemy` y tag de enemigo para que colision, graze y cleanup los traten como obstaculos jugables.
- El ultimo pilar puede actuar como pared continua cuando `spawnFinalContinuousWall` esta activo.
- En `ZonaAbisopelagica`, `GameRoot_ZonaAbisopelagica` instala `BossEventDirector` en `FlappyBossManager` para instanciar `UnknownBoss`.
- `UnknownBoss` declara `LightGrazeSource` propio para ser legible dentro de `LayerBlack`.
- La luz del boss puede ser eliptica y titilante; esto pertenece a su identidad visual de anguila electrica y no modifica dano, colision ni graze mecanico.

## SSCarnage

Archivos:
- `Assets/Implementation/Code/Bosses/SSCarnage/SSCarnageController.cs`
- `Assets/Implementation/Code/Bosses/SSCarnage/SSCarnageNetWall.cs`

Responsabilidad:
- Controlar el ciclo interno del boss SS Carnage.
- Posicionar al boss en fase de aviso sobre el `TopBoundary` del jugador.
- Desplegar la red que fuerza reaccion del jugador.
- Informar si el evento se resolvio o fallo.
- Retirarse hacia la derecha cuando termina su fase.

Contrato de cleanup:
- El prefab `SSCarnage` usa tag `SSCarnage` y conserva un `BoxCollider2D` trigger no jugable para que `DestroyOffscreen` pueda limpiarlo si queda atras.
- Ese collider no define dano ni graze; solo participa en limpieza fuera de camara.
- Si `BossNetWall` o el root de `SSCarnage` quedan fuera de camara y `DestroyOffscreen` los limpia durante `NetActive`, `SSCarnageController` interpreta el evento como resuelto. Este fallback evita que `RunProgressionDirector` quede bloqueado en `BossActive` y permite que el siguiente ciclo de Carnage vuelva a programarse.
- La distancia de aparicion de la red usa una regla proporcional a la velocidad horizontal actual del jugador: `max(netSpawnDistanceFromCameraRight, velocidadJugador * netHorizontalLeadTimeSeconds)`.
- `netSpawnDistanceFromCameraRight` es un piso minimo defensivo; `netHorizontalLeadTimeSeconds` cumple el mismo papel conceptual que el lead del anzuelo/cana, manteniendo una ventana de lectura estable cuando la velocidad crece.

Estados principales:
- `Inactive`
- `Warning`
- `DeployingNet`
- `NetActive`
- `Resolved`
- `Failed`
- `Exiting`
- `Finished`

## SSCarnageNetWall

La red usa `PlayerBoundaries` como fuente de altura:
- su borde inferior se coloca en el borde superior de `BottomBoundary`;
- su altura llega al borde inferior de `TopBoundary`;
- el volumen de colision se ajusta automaticamente;
- las capas visuales intactas se reemplazan por `BrokenNet` si el jugador resuelve el obstaculo con Ink-Pulse o `Shell Shield`.

La anchura fisica y las proporciones visuales proceden del prefab de autor; no se balancean con campos manuales de runtime. Al romperse, la red conserva su feedback visual y cambia a `BrokenNet`; no existe un flag local para destruirla.

El collider de `BossNetWall` permanece activo aunque la red este rota. La logica interna ignora nuevas colisiones cuando `isBroken` es verdadero, pero el collider debe seguir activo para que `DestroyOffscreen` pueda limpiar la red cuando queda fuera de camara.

El prefab puede incluir `AuthoringPlayerBoundaries` como referencia inactiva. Esta referencia define el tramo local authored que debe coincidir con los `PlayerBoundaries` reales de escena. No debe renombrarse a `PlayerBoundaries`, porque ese nombre queda reservado para la jerarquia runtime bajo `Boundaries`.

La escala de la red se calcula en espacio de mundo. Esto significa que el ajuste usa la altura fisica entre boundaries reales y compensa la escala heredada de padres, evitando diferencias entre la vista del prefab y Play.

## Flujo del boss

1. `RunProgressionDirector` permite iniciar boss.
2. `BossEventDirector` instancia `SSCarnage`.
3. `CameraController` entra en vista amplia.
4. `SSCarnageController` entra en `Warning`.
5. `SSCarnageController` despliega `BossNetWall`.
6. `SSCarnageNetWall` detecta si el jugador resolvio o fallo. Si la red o el root del boss fueron limpiados por quedar atras durante `NetActive`, `SSCarnageController` cierra el evento como resuelto.
7. `RunProgressionDirector` recibe `NotifyBossResolved()` o `NotifyBossFailed()`.
8. El boss sale o se destruye segun su configuracion.

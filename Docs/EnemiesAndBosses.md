# Enemigos y jefes

## Alcance

Este documento reúne el sistema de spawn, el catálogo de enemigos, el boss principal y su ciclo de resolución.

## LevelSpawner

Archivo: `Assets/Implementation/Code/Spawning/LevelSpawner.cs`

Responsabilidad:
- Generar enemigos y monedas según la progresión de la run.
- Ajustar intervalos y distribución vertical según intensidad y fronteras.
- Respetar la frecuencia de spawn que entrega `RunProgressionDirector`.
- Suspender el spawn normal sólo cuando `RunEventState` está en `Transitioning`.
- Instanciar enemigos únicamente desde `enemyProfiles`; no existe un prefab genérico de fallback.

Cada perfil de enemigo define prefab, tag lógico, peso de aparición, intensidad mínima y multiplicador de intervalo. El spawner aplica el tag con `EnemyTagCatalog` y fuerza la capa `Enemy` de forma recursiva al objeto instanciado.

Durante `BossActive`, el spawner no se detiene: recibe un intervalo reducido desde la progresión, por lo que los obstáculos aparecen con mayor frecuencia. Durante `PostBossWindow`, el intervalo aumenta para crear una ventana breve de reposo.

## EnemyTagCatalog

Archivo: `Assets/Implementation/Code/Spawning/EnemyTagCatalog.cs`

Responsabilidad:
- Centralizar tags lógicos de enemigos para evitar strings sueltos.

Regla:
- `PlayerCollision`, `GrazeDetector` y `DestroyOffscreen` consultan este catálogo para amenazas.
- Los tags no deben duplicarse como campos editables en esos componentes.

## Enemigos base

### PufferfishEnemy

Archivo: `Assets/Implementation/Code/Enemies/PufferfishEnemy.cs`

Responsabilidad:
- Comportamiento específico del pez globo como amenaza básica o de presión espacial.

### EnemySpawnContextReceiver

Archivo: `Assets/Implementation/Code/Enemies/EnemySpawnContextReceiver.cs`

Responsabilidad:
- Recibir referencias del contexto del spawn cuando un enemigo necesita cámara, fronteras o jugador.

## BossEventDirector

Archivo: `Assets/Implementation/Code/Bosses/BossEventDirector.cs`

Responsabilidad:
- Coordinar el arranque de eventos de boss.
- Entregar contexto de sesión, cámara, fronteras y progresión al boss activo.

## SSCarnage

Archivos:
- `Assets/Implementation/Code/Bosses/SSCarnage/SSCarnageController.cs`
- `Assets/Implementation/Code/Bosses/SSCarnage/SSCarnageNetWall.cs`

Responsabilidad:
- Controlar el ciclo interno del boss SS Carnage.
- Posicionar al boss en su fase de aviso.
- Desplegar la red o pared que fuerza la reacción del jugador.
- Informar si el evento se resolvió o falló.

Estados principales:
- `Inactive`
- `Warning`
- `DeployingNet`
- `NetActive`
- `Resolved`
- `Failed`
- `Exiting`
- `Finished`

## Flujo del boss

1. `BossEventDirector` inicia el evento.
2. `SSCarnageController` entra en aviso.
3. La red se despliega y pasa a estado activo.
4. `SSCarnageNetWall` detecta si el jugador resolvió o falló el evento.
5. `RunProgressionDirector` recibe `NotifyBossResolved()` o `NotifyBossFailed()`.
6. El boss sale o se destruye según el flujo configurado.

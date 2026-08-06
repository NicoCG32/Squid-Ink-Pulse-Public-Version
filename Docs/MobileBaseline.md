# Baseline funcional y técnico para el port móvil

## Propósito

Esta ficha congela una referencia Windows anterior a los cambios del port Android. Sus cifras permiten detectar regresiones y comparar builds posteriores, pero no constituyen presupuestos de rendimiento móvil.

Los binarios, logs y resultados XML se generan bajo `Build/` y `TestResults/`; ambas rutas permanecen fuera de Git.

## Snapshot

| Dato | Valor |
| --- | --- |
| Fecha | 5 de agosto de 2026 |
| Rama | `mobile/00-contract-baseline` |
| Commit de código medido | `79ce4221775424309889927985d082b1ad8fa661` |
| Unity | `6000.3.11f1` |
| Escenas activas | `MainMenu`, `ZonaEpipelagica`, `ZonaAbisopelagica`, `ShopMenu` |
| Escena fuera de Build Settings | `ZonaTutorial` |

El commit identifica el estado anterior a esta ficha. La documentación del baseline no modifica gameplay, escenas ni configuración de Player.

## Equipo Windows de referencia

La identificación del equipo, usuario, rutas personales y números de serie se omiten de forma intencional.

| Componente | Referencia |
| --- | --- |
| Sistema | Windows 11 Pro, versión `10.0.26200` |
| CPU | AMD Ryzen 5 7500F, 6 núcleos y 12 hilos |
| GPU | NVIDIA GeForce RTX 5060 Ti, driver `32.0.16.1088` |
| RAM visible | 31,6 GiB |
| Resolución de medición | 1280x720 en ventana; monitor 1920x1080 |

## Build comparable

Con el proyecto cerrado en otras instancias de Unity, la build limpia se generó desde la raíz del repositorio:

```powershell
New-Item -ItemType Directory -Force `
  "$PWD\Build\MobileBaselineWindows-79ce422", `
  "$PWD\TestResults" | Out-Null

& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -quit `
  -projectPath "$PWD" `
  -buildWindows64Player "$PWD\Build\MobileBaselineWindows-79ce422\SquidInkPulse.exe" `
  -logFile "$PWD\TestResults\windows-baseline-build-79ce422.log"
```

| Métrica | Resultado |
| --- | --- |
| Resultado Unity | `Success`, código de proceso `0` |
| Tiempo de pared | 149,95 s |
| Tiempo informado por `PlayerBuildInfo` | 114,417 s |
| Archivos | 201 |
| Tamaño total | 2.010.970.962 bytes; 1.917,81 MiB |
| Ejecutable | 667.648 bytes; 0,64 MiB |

Los cuatro archivos de recursos más grandes suman aproximadamente 1,68 GiB. Esta concentración justifica medir empaquetado Android antes de decidir cualquier compresión o reducción de assets.

## Arranque y memoria del proceso

Se inició el ejecutable limpio con `-screen-fullscreen 0 -screen-width 1280 -screen-height 720`. Un cronómetro de alta resolución comenzó inmediatamente antes de crear el proceso. Se consultó cada 100 ms hasta obtener una ventana con handle distinto de cero y estado receptivo; después se muestrearon `WorkingSet64` y `PrivateMemorySize64` durante 14 s. El proceso se cerró por su PID exacto al terminar la observación.

| Métrica | Resultado |
| --- | --- |
| Primera ventana receptiva | 6.697 ms |
| Working set a los 11,97 s | 558,36 MiB |
| Memoria privada a los 11,97 s | 1.219,67 MiB |
| Rango estable observado, 9,77–13,08 s | 558,20–558,48 MiB de working set |

La primera ventana receptiva es un proxy de arranque del proceso; no demuestra por sí sola que toda la navegación de `MainMenu` esté lista para interacción.

## Frame time y FPS

Para no incorporar instrumentación permanente al producto, se creó una copia instrumentada temporal y se retiró antes de la validación final. La sonda se activó únicamente por línea de comandos, esperó 3 s de calentamiento, registró `Time.unscaledDeltaTime` durante 10 s y cerró el Player. Se ejecutó una pasada en `MainMenu` y otra cargando directamente `ZonaEpipelagica`, en ventana oculta y sin foco a 1280x720.

| Escena | Frames / intervalo | FPS promedio | Frame time promedio | p95 | p99 | Memoria asignada / reservada de Unity |
| --- | --- | --- | --- | --- | --- | --- |
| `MainMenu` | 301 / 10,033 s | 30,00 | 33,332 ms | 33,367 ms | 33,367 ms | 108,10 / 180,80 MiB |
| `ZonaEpipelagica` | 301 / 10,033 s | 30,00 | 33,300 ms | 33,300 ms | 33,300 ms | 139,66 / 212,28 MiB |

El resultado exacto de 30 FPS caracteriza el Player oculto y sin foco usado para automatización. No demuestra el techo de rendimiento de una run visible ni sirve como presupuesto para Android. Una comparación posterior debe conservar calentamiento, intervalo, resolución, foco y escena; la candidata móvil requerirá además una run visible en hardware real.

## Validación automática y smoke

| Comprobación | Resultado |
| --- | --- |
| Suite EditMode | `126/126` aprobadas; 0 fallidas, omitidas o inconclusas |
| `SceneCompositionValidator.ValidateSceneComposition` | Código `0` |
| Build Windows limpia | Código `0` |
| Arranque de `MainMenu` | Ventana receptiva y proceso estable durante la observación |
| Carga técnica de `ZonaEpipelagica` | Sonda completada y salida natural con código `0` |
| Excepciones C# en las dos pasadas instrumentadas | No observadas |

Los Player logs de ambas escenas registraron un intento fallido de conexión a `localhost:8080` cuando no había servidor de feria. Es una observación reproducible de la base Windows y no impidió el arranque; el port Android debe mantener el add-on deshabilitado o inerte según [MobilePort.md](MobilePort.md).

## Smoke interactivo

El 5 de agosto de 2026 se ejecutó manualmente el standalone comparable del snapshot y se informó resultado correcto en todos los flujos requeridos:

| Flujo | Resultado |
| --- | --- |
| `MainMenu`, opciones, comic `Cómo Jugar` y tienda permanente | Correcto |
| Inicio de run, movimiento, graze, Ink-Pulse y gadget | Correcto |
| Pausa y reanudación | Correcto |
| Tienda temporal y comics del recorrido | Correcto |
| Portal hacia `ZonaAbisopelagica` | Correcto |
| Muerte, Game Over, retry y regreso a `MainMenu` | Correcto |
| Cierre, reapertura y persistencia | Correcto |

El resultado confirma el comportamiento observable del baseline Windows. Junto con la suite, el validador de composición y las mediciones técnicas, cierra la puerta funcional de la rama de baseline.

## Regla de comparación

Toda medición posterior debe registrar commit, Unity, plataforma, dispositivo, resolución, configuración de calidad, escena o flujo, calentamiento e intervalo. Las cifras obtenidas con otro dispositivo o condición se presentan como una serie distinta; no se sustituyen silenciosamente en esta tabla.

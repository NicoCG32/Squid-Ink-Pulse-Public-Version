# Pruebas automatizadas

## Alcance actual

La suite automatizada combina pruebas EditMode deterministas con una prueba PlayMode focalizada. EditMode cubre reglas de dominio, persistencia, contratos de escenas/prefabs, preparación Android, entrada mediante dispositivos simulados y el ownership de la superficie touch. PlayMode protege el timing de una solicitud de Ink-Pulse recibida durante pausa y confirma que se consume sin ejecutar ni reaparecer al reanudar.

Las pruebas viven en:

```text
Assets/Tests/EditMode/
Assets/Tests/PlayMode/
```

El código runtime se compila en `SquidInkPulse.Runtime`. Las asambleas de tests referencian esa frontera sin incluir pruebas en los builds normales. La rama de contrato de entrada usa además `Unity.InputSystem.TestFramework` para aislar dispositivos y estado global.

## Ejecución desde Unity

1. Abrir `Window > General > Test Runner`.
2. Ejecutar `EditMode` y luego `PlayMode`.
3. Confirmar que ambas pestañas terminan sin fallos, omitidas ni inconclusas.

## Ejecución batch en Windows

Cerrar antes cualquier instancia de Unity que tenga abierto el proyecto y ejecutar desde la raíz del repositorio:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -projectPath "$PWD" `
  -runTests `
  -testPlatform EditMode `
  -testResults "$PWD\TestResults\editmode-results.xml" `
  -logFile "$PWD\TestResults\editmode-unity.log"
```

El proceso debe devolver código `0`. El XML informa cantidad de pruebas ejecutadas, aprobadas, fallidas y omitidas.

Ejecutar PlayMode en un segundo proceso, después de EditMode:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -projectPath "$PWD" `
  -runTests `
  -testPlatform PlayMode `
  -testResults "$PWD\TestResults\playmode-results.xml" `
  -logFile "$PWD\TestResults\playmode-unity.log"
```

Las pruebas PlayMode no sustituyen el smoke del ejecutable ni la validación en hardware Android. Se reservan para interacciones cuyo orden depende del PlayerLoop y no puede demostrarse de forma honesta con una política pura.

Las pruebas EditMode de touch llaman el adaptador uGUI con `ExtendedPointerEventData` clasificado como `Touch` y verifican segundo dedo, UI interactiva, pausa, tienda, overlays, foco, suspensión y reemplazo de lector. El contrato del prefab comprueba además cuatro comandos exclusivos, ownership por botón, hit targets mínimos, superficie detrás de los botones y ausencia de Canvas/EventSystem/Collider propios. No afirma todavía que el orden real de `GraphicRaycaster` esté integrado en las zonas: ese gate requiere el montaje en GameRoot y el test PlayMode posterior.

`TestResults/` es un artefacto generado y no debe versionarse.

## Servidor de feria

El servidor opcional mantiene su smoke test independiente:

```powershell
python Tools\FairServer\smoke_test.py
```

Este script requiere que `Tools/FairServer/server.py` esté escuchando en `127.0.0.1:8080`.

## Extensión posterior

Las siguientes incorporaciones deben priorizar caracterización de flujos que se vayan a refactorizar. No se deben añadir tests masivamente sin identificar primero qué contrato protegen.

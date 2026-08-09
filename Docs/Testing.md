# Pruebas automatizadas

## Alcance actual

La suite automatizada combina pruebas EditMode deterministas con dos pruebas PlayMode focalizadas. EditMode cubre reglas de dominio, persistencia, contratos de escenas/prefabs, preparación Android, entrada mediante dispositivos simulados, autoscroll sin target vertical, escala extra-wide, safe area y ownership de la superficie touch. PlayMode protege el timing de una solicitud recibida durante pausa y también ejerce el routing real de `InputSystemUIInputModule` con un touchscreen virtual a través de Epipelágica y Abisopelágica.

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

Las pruebas EditMode de touch llaman el adaptador uGUI con `ExtendedPointerEventData` clasificado como `Touch` y verifican segundo dedo, UI interactiva, pausa, tienda, overlays, foco, suspensión y reemplazo de lector. El contrato del prefab comprueba además cuatro comandos exclusivos, la etiqueta `INK-PULSO`, ownership por botón, hit targets mínimos, superficie detrás de los botones y ausencia de Canvas/EventSystem/Collider propios. El contrato de GameRoot exige una instancia anidada del mismo prefab en Epipelágica y Abisopelágica, cero en tutorial, referencias HUD intactas, una sola raíz de safe area y ningún EventSystem o módulo de entrada adicional. PlayMode confirma el primer raycast efectivo, steering, Ink-Pulse, pausa y recreación del reader al cargar Abisopelágica. Esto no sustituye la ergonomía multitouch, la orientación opuesta ni una run completa en hardware.

`TestResults/` es un artefacto generado y no debe versionarse.

## Servidor de feria

El servidor opcional mantiene su smoke test independiente:

```powershell
python Tools\FairServer\smoke_test.py
```

Este script requiere que `Tools/FairServer/server.py` esté escuchando en `127.0.0.1:8080`.

## Extensión posterior

Las siguientes incorporaciones deben priorizar caracterización de flujos que se vayan a refactorizar. No se deben añadir tests masivamente sin identificar primero qué contrato protegen.

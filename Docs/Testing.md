# Pruebas automatizadas

## Alcance actual

La suite automatizada combina pruebas EditMode deterministas con cinco pruebas PlayMode focalizadas. EditMode cubre reglas de dominio, persistencia, contratos de escenas/prefabs, preparación Android, entrada mediante dispositivos simulados, autoscroll sin target vertical, escala extra-wide, safe area, layouts responsive de Main Menu/tienda y ownership de la superficie touch. PlayMode protege el timing de una solicitud recibida durante pausa, ejerce el routing real de `InputSystemUIInputModule` con un touchscreen virtual a través de Epipelágica y Abisopelágica, y comprueba encuadre y posiciones de spawn en los aspectos móviles.

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

El fixture de routing touch alinea su reloj simulado con `Time.realtimeSinceStartupAsDouble` al comenzar. Unity descarta eventos fechados durante la transición del Editor a Play Mode; reiniciar el reloj a cero entre fixtures podía colocar touches válidos dentro de ese intervalo y hacer fallar la suite conjunta aunque la prueba aislada pasara. Se conservan los asserts de routing y los tiempos de un frame, sin modificar políticas runtime de foco ni silenciar errores.

Las pruebas EditMode de touch llaman el adaptador uGUI con `ExtendedPointerEventData` clasificado como `Touch` y verifican segundo dedo, UI interactiva, pausa, tienda, overlays, foco, suspensión y reemplazo de lector. El contrato del prefab comprueba además cuatro comandos exclusivos, la etiqueta `INK-PULSO`, ownership por botón, hit targets mínimos, superficie detrás de los botones y ausencia de Canvas/EventSystem/Collider propios. El contrato de GameRoot exige una instancia anidada del mismo prefab en Epipelágica y Abisopelágica, cero en tutorial, referencias HUD intactas, una sola raíz de safe area y ningún EventSystem o módulo de entrada adicional. PlayMode confirma el primer raycast efectivo, steering, Ink-Pulse, pausa y recreación del reader al cargar Abisopelágica. Esto no sustituye la ergonomía multitouch, la orientación opuesta ni una run completa en hardware.

`MobileMenuLayoutContractTests` congela las decisiones que dependen del formato: los cuatro Canvas del cómic `Cómo Jugar` preservan los 1080 de alto, `Character` mantiene exactamente 3/4 de su ancho dentro del borde derecho y los letreros de categorías de `ShopMenu` conservan proporción 3:1 y margen derecho sin stretch.

Las pruebas de HUD, escala y touch consumen el inventario de `MobileGameRootCatalog`, compartido con el validador de composición. El contrato HUD compara geometría y fuente anidada con Epipelágica; seis casos negativos alteran en memoria posición de InkBar, anchor de score, tamaño de controles y escala, rotación u offset de la raíz segura para verificar que se rechazan divergencias. Esas instancias se descargan sin guardar prefabs. La comprobación de escenas carga sus instancias efectivas, incluidas sustituciones locales de elementos HUD. Compilar los tests con un compilador C# externo no equivale a ejecutarlos en Unity ni valida legibilidad en teléfono.

`TestResults/` es un artefacto generado y no debe versionarse.

`MobileCameraFramingPlayModeTests` carga las dos escenas reales en 16:9, 19.5:9 y 20:9. Mantiene al jugador inmóvil y desactiva spawners/progresión para aislar geometría mientras `CameraController` sigue ejecutándose en el PlayerLoop. Comprueba cuerpo y graze en los extremos verticales y el centro, viewport dentro de `CameraBoundaries`, estabilidad al deshabilitar el trigger de graze, vista amplia y regreso a seguimiento. Con el perfil de spawn de cada escena, verifica camarones en la intersección vertical visible, portales/enemigos dentro del rango del jugador y todos los puntos desde el borde derecho de cámara. Estas pruebas aíslan el Input System mediante `InputTestFixture` y restauran el estado aleatorio al muestrear spawn; no certifican una run, rendimiento ni ergonomía.

## Matriz visual estática del HUD

`Tools > Squid Ink Pulse > Mobile > Export HUD Preview Matrix` carga las dos escenas de gameplay y exporta doce PNG, una galería HTML y medidas JSON a `TestResults/MobileHudPreview/`. Requiere Edit Mode, escenas sin cambios sin guardar y un dispositivo gráfico disponible. Descarta las modificaciones de previsualización y restaura las escenas abiertas; no guarda escenas ni prefabs.

También puede ejecutarse desde la raíz del repositorio, con el proyecto cerrado en otras instancias de Unity:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode -quit -projectPath "$PWD" `
  -executeMethod MobileHudPreviewExporter.Export `
  -logFile "$PWD\TestResults\hud-preview.log"
```

Este comando necesita gráficos: omitir `-nographics`. Incluye 1920×1080 (16:9), 2340×1080 (19.5:9), 2400×1080 (20:9), cutouts laterales de 108 px en ambos sentidos y 2712×1220 con inset izquierdo de 122 px. Los cutouts son simulados, no mediciones de un teléfono.

La captura usa la escena de autoría, Canvas sobre cámara y escalado por altura, barra llena y controles touch visibles. Comprueba bounds dentro del área segura y solapamientos entre los cuatro botones; excluye la superficie de steering que ocupa toda la pantalla. Las intersecciones entre contenedores HUD se registran como observaciones que requieren inspeccionar contenido visible: sus rects pueden incluir espacio transparente. Las observaciones TMP distinguen texto fuera de su rect de clipping: `Overflow` permite mostrar glifos fuera del rect. No se ejecutan animaciones, presentación de estados gameplay, routing de entrada ni una run; tampoco se certifican framing dinámico, ergonomía o rendimiento móvil.

`SafeAreaAdapterTests` carga además los prefabs reales con destino serializado sin asignar y exige que `Apply` resuelva el RectTransform local. Este caso protege la semántica de null de objetos Unity, que no equivale siempre a null CLR en el Editor.

## Servidor de feria

El servidor opcional mantiene su smoke test independiente:

```powershell
python Tools\FairServer\smoke_test.py
```

Este script requiere que `Tools/FairServer/server.py` esté escuchando en `127.0.0.1:8080`.

## Extensión posterior

Las siguientes incorporaciones deben priorizar caracterización de flujos que se vayan a refactorizar. No se deben añadir tests masivamente sin identificar primero qué contrato protegen.

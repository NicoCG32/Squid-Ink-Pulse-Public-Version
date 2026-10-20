# Pruebas automatizadas

## Alcance actual

La suite automatizada combina pruebas EditMode deterministas con ocho pruebas PlayMode focalizadas. EditMode cubre reglas de dominio, persistencia, contratos de escenas/prefabs, preparación Android, entrada mediante dispositivos simulados, autoscroll sin target vertical, escala extra-wide, safe area, layouts responsive de Main Menu/tienda y ownership de la superficie touch. PlayMode protege el timing de una solicitud recibida durante pausa, ejerce el routing real de `InputSystemUIInputModule` con un touchscreen virtual a través de Epipelágica y Abisopelágica, comprueba encuadre y posiciones de spawn en los aspectos móviles y recorre opciones/tutorial/tienda/Jugar desde MainMenu sin mouse. También verifica en la escena real las opciones de display y la presentación desktop de la tienda.

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

`MainMenuTouchNavigationPlayModeTests` carga MainMenu y usa touches reales del módulo UI para abrir/cerrar opciones, recorrer las tres páginas de Cómo Jugar, cargar ShopMenu y volver mediante su botón. Después toca Jugar y el botón explícito del cómic inicial hasta entrar a Epipelágica con HUD activo y `Time.timeScale = 1`; comprueba que el cómic mantiene el tiempo pausado antes de continuar. Comprueba raycast y estado pressed durante Down, sin mouse. Espera la actualización de UI tras activar el tutorial o una escena antes de comprobar los botones recién creados; no cambia la apariencia ni despacha OnClick directamente.

El fixture de routing touch alinea su reloj simulado con `Time.realtimeSinceStartupAsDouble` al comenzar. Unity descarta eventos fechados durante la transición del Editor a Play Mode; reiniciar el reloj a cero entre fixtures podía colocar touches válidos dentro de ese intervalo y hacer fallar la suite conjunta aunque la prueba aislada pasara. Se conservan los asserts de routing y los tiempos de un frame, sin modificar políticas runtime de foco ni silenciar errores.

`OptionsPlatformPresentationTests` carga el prefab real de opciones: simula la presentación móvil/desktop, comprueba los dos controles de display ocultos sólo en móvil y mantiene visibles volumen/regreso, con valores y posiciones intactos. Reaplica la política y restaura presentación desktop; no guarda el prefab ni escribe preferencias del jugador. Complementa las pruebas puras ya existentes de resolución.

Las pruebas EditMode de touch llaman el adaptador uGUI con `ExtendedPointerEventData` clasificado como `Touch` y verifican segundo dedo, UI interactiva, pausa, tienda, overlays, foco, suspensión y reemplazo de lector. El contrato del prefab comprueba además cuatro comandos exclusivos, la etiqueta `INK-PULSO`, ownership por botón, hit targets mínimos, superficie detrás de los botones y ausencia de Canvas/EventSystem/Collider propios. El contrato de GameRoot exige una instancia anidada del mismo prefab en Epipelágica y Abisopelágica, cero en tutorial, referencias HUD intactas, una sola raíz de safe area y ningún EventSystem o módulo de entrada adicional. PlayMode confirma el primer raycast efectivo, steering, Ink-Pulse, pausa y recreación del reader al cargar Abisopelágica. Esto no sustituye la ergonomía multitouch, la orientación opuesta ni una run completa en hardware.

`MobileMenuLayoutContractTests` congela las decisiones que dependen del formato: los cuatro Canvas del cómic `Cómo Jugar` preservan los 1080 de alto, `Character` mantiene exactamente 3/4 de su ancho dentro del borde derecho y los letreros de categorías de `ShopMenu` conservan proporción 3:1 y margen derecho sin stretch.

Las pruebas de HUD, escala y touch consumen el inventario de `MobileGameRootCatalog`, compartido con el validador de composición. El contrato HUD compara geometría y fuente anidada con Epipelágica; seis casos negativos alteran en memoria posición de InkBar, anchor de score, tamaño de controles y escala, rotación u offset de la raíz segura para verificar que se rechazan divergencias. Esas instancias se descargan sin guardar prefabs. La comprobación de escenas carga sus instancias efectivas, incluidas sustituciones locales de elementos HUD. Compilar los tests con un compilador C# externo no equivale a ejecutarlos en Unity ni valida legibilidad en teléfono.

`TestResults/` es un artefacto generado y no debe versionarse.

`MobileCameraFramingPlayModeTests` carga las dos escenas reales en 16:9, 19.5:9 y 20:9. Mantiene al jugador inmóvil y desactiva spawners/progresión para aislar geometría mientras `CameraController` sigue ejecutándose en el PlayerLoop. Comprueba cuerpo y graze en los extremos verticales y el centro, viewport dentro de `CameraBoundaries`, estabilidad al deshabilitar el trigger de graze, vista amplia y regreso a seguimiento. Con el perfil de spawn de cada escena, verifica camarones en la intersección vertical visible, portales/enemigos dentro del rango del jugador y todos los puntos desde el borde derecho de cámara. Estas pruebas aíslan el Input System mediante `InputTestFixture` y restauran el estado aleatorio al muestrear spawn; no certifican una run, rendimiento ni ergonomía.

## Matriz visual estática del HUD

`Tools > Squid Ink Pulse > Mobile > Export HUD Preview Matrix` carga las dos escenas de gameplay y exporta catorce PNG, una galería HTML y medidas JSON a `TestResults/MobileHudPreview/`. Requiere Edit Mode, escenas sin cambios sin guardar y un dispositivo gráfico disponible. Descarta las modificaciones de previsualización y restaura las escenas abiertas; no guarda escenas ni prefabs.

También puede ejecutarse desde la raíz del repositorio, con el proyecto cerrado en otras instancias de Unity:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode -quit -projectPath "$PWD" `
  -executeMethod MobileHudPreviewExporter.Export `
  -logFile "$PWD\TestResults\hud-preview.log"
```

Este comando necesita gráficos: omitir `-nographics`. Incluye 1920×1080 (16:9), 2340×1080 (19.5:9), 2400×1080 (20:9), cutouts laterales de 108 px en ambos sentidos y 2712×1220 con inset izquierdo de 122 px. Los cutouts son simulados, no mediciones de un teléfono.

La captura usa la escena de autoría, Canvas sobre cámara y la política compartida de escala, barra llena y controles touch visibles. Comprueba bounds dentro del área segura y solapamientos entre los cuatro botones; excluye la superficie de steering que ocupa toda la pantalla. Las intersecciones entre contenedores HUD se registran como observaciones que requieren inspeccionar contenido visible: sus rects pueden incluir espacio transparente. Las observaciones TMP distinguen texto fuera de su rect de clipping: `Overflow` permite mostrar glifos fuera del rect. No se ejecutan animaciones, presentación de estados gameplay, routing de entrada ni una run; tampoco se certifican framing dinámico, ergonomía o rendimiento móvil.

`SafeAreaAdapterTests` carga además los prefabs reales con destino serializado sin asignar y exige que `Apply` resuelva el RectTransform local. Este caso protege la semántica de null de objetos Unity, que no equivale siempre a null CLR en el Editor.

La matriz incorpora también 2560×1600 (tablet 16:10), con catorce capturas de autoría en total. Las pruebas de cámara incluyen ese aspecto además de los tres formatos de teléfono. `MobileMenuCanvasScalePolicyTests` comprueba que el viewport lógico contenga la composición de referencia: ancho para superficies landscape más estrechas que 16:9, altura para las equivalentes o más anchas. Dimensiones portrait iniciales se normalizan a landscape; Windows conserva su match de autoría y los tamaños inválidos no fuerzan un cambio.

## Matriz visual estática del menú

`Tools > Squid Ink Pulse > Mobile > Export Main Menu Preview Matrix` (o batch `-executeMethod MainMenuPreviewExporter.Export`, con gráficos) exporta seis PNG y `report.json` a `TestResults/MainMenuPreview/`. Cubre 16:9, 20:9, tablet 16:10 y márgenes laterales simulados. Aplica escala/presentación móvil en memoria, oculta opciones/tutorial/Salir y mide los cuatro botones visibles, logo y contador. No guarda la escena; restaura la sesión del Editor. Los 36 bounds complementan los tests de jerarquía y la navegación PlayMode, sin certificar animación, ergonomía ni una run.

## Matriz visual de Cómo Jugar

`MainMenuTutorialPreviewExporter.Export` (menú `Tools > Squid Ink Pulse > Mobile > Export Main Menu Tutorial Preview Matrix`) requiere gráficos y genera dieciocho PNG, JSON y galería en `TestResults/MainMenuTutorialPreview/`. Recorre las tres páginas reales en 16:9/20:9, con márgenes laterales simulados en ambos sentidos, y tablet 16:10. Aplica la misma política de escala, área segura y ajuste de referencia móvil sin guardar la escena. Comprueba los quads de glifos TMP emitidos y el hit area de Next contra el área segura; los quads pueden incluir padding SDF. No convierte el overflow del rect de texto en clipping ni certifica legibilidad, arte o routing físico.

`MobileReferenceContentFitterTests` comprueba que la composición completa quepa con márgenes simétricos, conserve proporción al redimensionar y no acumule transformaciones. Desktop y tamaños inválidos preservan la geometría previa. El contrato de escena comprueba la jerarquía segura, las referencias de páginas/Next y la ausencia de Canvas adicional.

## Matriz de opciones

`OptionsMenuPreviewExporter.Export`, también disponible en `Tools > Squid Ink Pulse > Mobile > Export Options Preview Matrix`, requiere gráficos y genera 24 PNG, galería y JSON bajo `TestResults/OptionsMenuPreview/`. Carga el prefab efectivo en MainMenu, ShopMenu, Epipelágica y Abisopelágica. Cubre 16:9, 20:9 con cutouts laterales y tablet 16:10; comprueba cuatro rects de controles/placas por captura, los extremos y centro del handle de volumen y cobertura completa del fondo. Descarta cambios sin guardar escenas, assets ni preferencias. No certifica animación, routing o ergonomía en hardware.

Los tests de opciones inspeccionan prefab y cuatro instancias de escena, incluyendo el Canvas anidado de MainMenu y su ajuste al cambiar el viewport. El validador prebuild comprueba jerarquía, referencias, dimmer y ausencia de Canvas/raycaster extra. El fixture PlayMode del menú añade raycast de fondo en una esquina y ajuste del slider con touchscreen virtual; durante esa comprobación desconecta/restaura sólo el listener de persistencia para no escribir las preferencias del usuario del Editor.

`MobileTutorialCopyTests` protege overrides parciales, reaplicación y retorno a la copia desktop. El contrato de la escena exige instrucciones de arrastre/INK-PULSO y la matriz del tutorial renderiza las variantes móviles reales, conservando las ilustraciones.

## Tienda permanente móvil

`PermanentShopPreviewExporter.Export` genera `TestResults/PermanentShopPreview/index.html` y `report.json` desde la escena real. Cubre ocho formatos: 16:9 sin cutout/con cutout izquierdo/derecho, 19,5:9 y 20:9 con cutout en ambos lados, y tablet 16:10. Comprueba fondo completo, trece hitboxes dentro del área segura y un mínimo diagnóstico de 48 píxeles de preview por lado; esa medida no certifica tamaño físico ni ergonomía. Recorre los nombres, descripciones y precios de las cuatro mejoras y ocho skins visibles del catálogo semilla, midiendo glifos TMP y overflow. Carga sprites mediante el presentador de slots y no carga ni guarda el perfil del usuario. Las capturas son evidencia complementaria: no prueban routing ni compras.

`PermanentShopTouchPlayModeTests` carga ShopMenu y usa touchscreen virtual, raycast real y feedback pressed sin mouse. Recorre compra de mejora/skin, equipar/quitar sin descuento adicional, paginación que limpia selección, falta de saldo y exclusión de una entrada futura sin sprite. La acción Cancel del módulo UI cierra opciones antes de regresar al menú. El fixture inyecta perfil/registros/catálogo en memoria, exige las escrituras transitorias de Editor y restaura el estado al terminar; no acredita fondos ni compra sobre datos del usuario. No se ejecuta en players. La revisión de Back Android físico, orientaciones y ciclo de vida sigue siendo independiente.

El caso desktop del mismo fixture conserva la presentación de autoría: nombre sin auto size móvil y aviso de compra móvil oculto. Abre opciones desde ShopMenu, exige resolución/fullscreen visibles y comprueba que Escape no active la navegación Cancel exclusiva de móvil. Cierra opciones y regresa a MainMenu mediante los botones existentes; no cambia resolución, fullscreen ni preferencias. Complementa las pruebas de políticas y la compilación/smoke Windows, sin certificar interacción física de teclado/mouse.

`PermanentShopMobileCompositionTests` protege los contenedores, contador, ausencia de infraestructura UI adicional, referencias de navegación y feedback de compra oculto por defecto. PlayMode comprueba además que el TMP visible refleja `EQUIPADA` y `SIN CAMARONES`; el exporter valida los estados largos dentro del área segura. `MobileBackgroundCoverTests` comprueba cobertura proporcional, repetición/redimensionado y preservación de autoría desktop.

La cobertura de menú y tienda se reparte entre verificaciones complementarias:

| Contrato | Evidencia automatizada | Complemento visual/físico |
| --- | --- | --- |
| Opciones por plataforma | Política, prefab y cuatro instancias; PlayMode móvil/desktop | Matriz de opciones y volumen en tablet |
| Tutorial y entrada a run | Touch sin mouse, tres páginas, regreso, Jugar y cómic inicial | Matrices de menú/tutorial y recorrido en tablet |
| Compra, equipamiento y navegación | Touch, perfil transitorio, rechazo, paginación y Cancel contextual | Selección, precios, rechazo sin saldo y Back Android en tablet |
| Safe area y referencias | Contratos de escenas/prefabs y validador prebuild | Matrices con cutouts simulados y tablet 16:10 |
| Regresión Windows | Presentación desktop y opciones de display en escena real | Build standalone y smoke de arranque |

Las matrices no sustituyen routing, los dispositivos virtuales no certifican ergonomía y el smoke de arranque no acredita una run interactiva. La evidencia de builds y tablet, con sus límites, se registra en [AndroidBuild.md](AndroidBuild.md).

## Servidor de feria

El servidor opcional mantiene su smoke test independiente:

```powershell
python Tools\FairServer\smoke_test.py
```

Este script requiere que `Tools/FairServer/server.py` esté escuchando en `127.0.0.1:8080`.

## Extensión posterior

Las siguientes incorporaciones deben priorizar caracterización de flujos que se vayan a refactorizar. No se deben añadir tests masivamente sin identificar primero qué contrato protegen.

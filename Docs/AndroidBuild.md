# Build e instalación Android

## Estado

El proyecto dispone de un comando reproducible para generar un APK Development. La compilación, inspección estática, instalación y apertura de `MainMenu` están verificadas en un teléfono Android ARM64 mediante ADB.

Abrir el juego en Editor no sustituye esta prueba. La puerta de bootstrap Android sólo se cierra cuando el APK generado se instala y abre en Android sin un crash bloqueante.

## Requisitos locales

- Unity `6000.3.11f1`;
- Android Build Support de esa misma instalación;
- SDK, NDK y OpenJDK instalados mediante Unity Hub;
- las cuatro escenas obligatorias habilitadas en Build Settings;
- para instalar, un teléfono con depuración USB autorizada o un emulador visible en `adb devices`.

Las rutas locales del SDK y JDK no se guardan en el proyecto.

## Generar el APK

### Desde el menú

```text
Tools > Squid Ink Pulse > Build Android Development APK
```

Si el Editor está en otra plataforma, el comando cambia temporalmente a Android y restaura el target anterior al terminar.

### Desde PowerShell

Cerrar antes cualquier otra instancia de Unity que tenga abierto el proyecto. Desde la raíz del repositorio:

```powershell
New-Item -ItemType Directory -Force "$PWD\TestResults" | Out-Null

& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -quit `
  -projectPath "$PWD" `
  -buildTarget Android `
  -executeMethod AndroidDevelopmentBuilder.BuildAndroidDevelopmentApk `
  -logFile "$PWD\TestResults\android-development-build.log"
```

El argumento `-buildTarget Android` es obligatorio en batch. El proceso devuelve un código distinto de cero si falta Android Build Support, una escena requerida, una ruta válida o si Unity informa un build fallido.

## Salida

El builder es propietario de esta carpeta y la limpia antes de cada ejecución:

```text
Build/AndroidDevelopment/SquidInkPulse-development.apk
```

`Build/` y `*.apk` están ignorados por Git. No se deben agregar binarios Android al repositorio.

El APK siempre se solicita con `BuildOptions.Development` y `AllowDebugging`; no es una candidata firmada para publicación. El postprocesador de feria se limita a Windows y no genera guías `.exe` ni scripts junto al APK.

## Verificar ADB

El SDK incluido con Unity contiene ADB. En PowerShell:

```powershell
$AndroidSdkRoot = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK'
$Adb = Join-Path $AndroidSdkRoot 'platform-tools\adb.exe'

& $Adb version
& $Adb devices -l
```

El dispositivo debe aparecer con estado `device`. Si aparece `unauthorized`, se debe desbloquear el teléfono y aceptar la huella RSA. Una lista vacía no permite validar instalación ni arranque.

## Instalar y abrir

Con un único dispositivo conectado:

```powershell
$Apk = Join-Path $PWD 'Build\AndroidDevelopment\SquidInkPulse-development.apk'

& $Adb install -r $Apk
& $Adb shell monkey `
  -p com.yecoworks.squidinkpulse `
  -c android.intent.category.LAUNCHER `
  1
```

`adb install -r` conserva datos de una instalación compatible. Para probar una instalación realmente limpia se debe desinstalar antes el package; `adb uninstall com.yecoworks.squidinkpulse` elimina también sus datos locales y sólo debe usarse de forma intencional.

### APK Development del contrato HUD común — 30 de septiembre de 2026

Se generó un APK desde `mobile/04-touch-controls`, con base `c182926` y cambios locales de consolidación del HUD aún sin commit. El marcador de commit del builder no identifica por sí solo este árbol modificado.

- Resultado: `Succeeded`; 0 errores, 1 advertencia; 56,58 s informados por el builder.
- Tamaño: 514.308.880 bytes (490,48 MiB).
- SHA-256: `93F74572BE290A26206D58AF1D200374D71458E718920566BE4BDC1BBD5CA4B1`.
- EditMode: 253/253; PlayMode: 2/2; composición de escenas y prefabs aprobada en Unity 6000.3.11f1.
- Las escenas reubican los elementos HUD locales bajo la raíz segura y alinean la posición de InkBar abisal con el contenedor canónico.
- En ese primer pase no se instaló: ADB no detectó un dispositivo y se continuó sin hardware. Este artefacto fue sustituido por el rebuild siguiente. Es un APK de desarrollo, sin aceptación como candidata final.

La continuación con matriz visual detectó un fallback defectuoso de `SafeAreaAdapter` al cargar referencias serializadas sin asignar en el Editor. Ahora resuelve el RectTransform local usando la comparación null de Unity; las dos regresiones de prefab fallaron antes del fix y pasan después.

- Rebuild actualizado: `Succeeded`; 0 errores, 2 advertencias; 92,43 s; 514.317.290 bytes (490,49 MiB).
- SHA-256: `0A0921A1B6D3077BCEB5AA554FE32BCB3343CA293BC307191A3DB8AF4241EC41`.
- EditMode: 255/255; PlayMode: 2/2; composición aprobada durante prebuild.
- Matriz de autoría: doce capturas, 96 bounds dentro de safe area y ningún solapamiento entre botones. Las intersecciones de contenedores transparentes y avisos TMP se conservan como observaciones; no equivalen a clipping demostrado.
- El usuario conectó y autorizó el POCO X6 5G, Android 16, pantalla física 1220×2712. `adb install -r` terminó con `Success`; el APK abrió `MainMenu` en landscape 2712×1220.
- La inyección `adb shell input tap` fue rechazada por Android por falta de `INJECT_EVENTS`. El recorrido manual y la aceptación del HUD se verifican por separado; la instalación y una captura de menú no certifican una run ni ergonomía.

El 1 de octubre de 2026 se capturó Epipelágica durante una run pausada en el POCO: barra, gadgets, camarones, score y controles completos dentro del frame. El botón de pausa pasó a `REANUDAR`, y el menú de pausa también quedó visible completo. El usuario confirmó que ambas zonas se ven bien y los controles resultan cómodos. La captura directa de este pase corresponde a Epipelágica; la aceptación de Abisopelágica fue manual. No se observan coincidencias críticas en el log filtrado del proceso.

El build Windows complementario terminó correctamente y el ejecutable a 1280×720 se mantuvo vivo y responsivo durante un smoke de 15 s, sin excepciones observadas en el log del player. Ese smoke comprueba arranque; no se presenta como una run interactiva de teclado/mouse. La aceptación de este HUD no cierra rendimiento sostenido, ciclo de vida, ambas orientaciones ni el candidato Android completo.

### APK Development del ajuste de cámara — 1 de octubre de 2026

Se recompiló desde `mobile/05-safe-area-hud`, base `d72a31e` más el ajuste local de cámara y sus pruebas. El árbol conservaba también un cambio previo de `TimeManager.asset` fuera del commit de cámara; el APK no se atribuye a un checkout limpio de esa base.

- Resultado: `Succeeded`; 0 errores, 2 advertencias; 229,31 s informados por el builder.
- Tamaño: 514.324.008 bytes (490,50 MiB).
- SHA-256: `42C8B6887CA1ED758833D600063CA64A0DD289F4B38355E8A237269B1C6845A7`.
- EditMode: 255/255; PlayMode: 5/5, sin fallos ni omitidas; composición aprobada en prebuild Android y Windows.
- Las pruebas de cámara reproducían un recorte izquierdo del graze en Follow estabilizado a 16:9. El ajuste limita el offset sólo cuando hace falta espacio; comprueba ambas zonas, límites verticales, vista amplia/regreso y posiciones de spawn en 16:9, 19.5:9 y 20:9.
- `adb install -r` terminó con `Success`, conservando datos; actividad del juego en primer plano y captura de MainMenu a 2712×1220. El log filtrado del proceso no mostró excepciones críticas. Esta captura verifica arranque, no una nueva run; la aceptación manual del HUD de ambas zonas corresponde al pase anterior.
- Build Windows correcto; smoke oculto de arranque durante 15 s, vivo y responsivo, sin excepciones C# observadas. Se solicitó 1280×720 en ventana, pero las opciones guardadas reaplicaron fullscreen y DXGI informó fallback a la resolución del display (3840×2160). Este smoke no certifica el tamaño efectivo de la ventana ni una run interactiva; el encuadre se verifica con PlayMode.

## Capturar logs

Limpiar el buffer inmediatamente antes de abrir el juego y guardar sólo el log técnico necesario:

```powershell
New-Item -ItemType Directory -Force "$PWD\TestResults" | Out-Null

& $Adb logcat -c
& $Adb shell monkey `
  -p com.yecoworks.squidinkpulse `
  -c android.intent.category.LAUNCHER `
  1

& $Adb logcat -d -v threadtime `
  'Unity:I' `
  'AndroidRuntime:E' `
  '*:S' |
  Set-Content "$PWD\TestResults\android-mainmenu-logcat.txt"
```

Antes de compartir un log se deben revisar y retirar identificadores del dispositivo, rutas personales, direcciones de red y datos del perfil. `TestResults/` permanece ignorado.

La validación mínima de arranque debe confirmar:

1. instalación terminada con `Success`;
2. icono y label `Squid Ink-Pulse` disponibles;
3. aplicación visible en landscape;
4. `MainMenu` renderizado y receptivo;
5. ausencia de `FATAL EXCEPTION`, crash nativo o cierre inmediato en logcat;
6. cierre y segunda apertura correctos.

## Evidencia del primer APK reproducible

Build ejecutado el 8 de agosto de 2026 desde `mobile/01-android-bootstrap`:

| Dato | Resultado |
| --- | --- |
| Commit de código | `43ed9dd` |
| Unity | `6000.3.11f1` |
| Resultado | `Succeeded`; 0 errores |
| Tiempo de pared cacheado | 63,61 s |
| Tiempo informado por el builder | 55,96 s |
| Tamaño | 449.819.825 bytes; 428,98 MiB |
| SHA-256 local | `29AC2030F40F288ED02D5F9323C2BA44C4E4967DE5CF77DC33C9C767742E1C86` |
| Package | `com.yecoworks.squidinkpulse` |
| Version code / version name | `1` / `1.0` |
| Min SDK / Target SDK / Compile SDK | `25` / `36` / `36` |
| ABI | `arm64-v8a` |
| Firma | Certificado debug; APK Signature Scheme v2 verificado |
| Archivos en la carpeta de salida | Sólo `SquidInkPulse-development.apk` |

El primer pase sin caché de esta rama tardó 1.080,20 s de pared y generó la caché IL2CPP ARM64. Esa cifra no se compara directamente con rebuilds cacheados.

## Evidencia de instalación y arranque

El mismo APK se instaló y abrió el 8 de agosto de 2026. No se registran número de serie, cuentas, direcciones de red ni otros identificadores personales.

| Dato | Resultado |
| --- | --- |
| Dispositivo | POCO X6 5G |
| Sistema | Xiaomi HyperOS `OS3.0`; build `OS3.0.302.0.WNRMIXM` |
| Android | Android 16; API 36 |
| RAM visible | 11,02 GiB |
| Pantalla física | 1220x2712; densidad 480 |
| ABI primaria | `arm64-v8a` |
| Instalación | `adb install -r`; `Success` en 26,54 s |
| Primer arranque | Proceso activo y `UnityPlayerGameActivity` en primer plano tras 20 s |
| Segundo arranque | Proceso activo y actividad en primer plano tras 12 s |
| Crashes / ANR | Sin `FATAL EXCEPTION`, señal nativa ni ANR observados |
| Orientación capturada | Landscape; screenshot 2712x1220 |

`MainMenu` se renderizó y permaneció como actividad en primer plano. La captura inicial de bootstrap mostró, sin embargo, el contenido comprimido en una franja central con grandes bandas grises arriba y abajo. La causa se corrigió posteriormente como bloqueo prioritario del port; la evidencia actual se describe en la sección siguiente.

Logcat del primer arranque registró:

- creación pendiente de la caché Vulkan en el primer inicio;
- avisos no fatales del runtime Unity al configurar afinidad de CPU;
- `Curl error 7` al intentar conectar a `localhost:8080`.

El probe de red demuestra que el add-on de feria todavía no está inerte en Android. No produjo crash ni impidió `MainMenu`, pero debe eliminarse o deshabilitarse antes de una candidata móvil.

### Observaciones del build

- no se observaron errores bloqueantes de shaders, plugins, stripping, IL2CPP ni Gradle;
- el BuildReport cacheado registró un warning y cero errores; el log resumido no mostró un warning accionable perteneciente al código del juego;
- la primera compilación nativa mostró warnings internos del debugger Mono/IL2CPP del toolchain de Unity, pero terminó correctamente;
- el manifiesto solicita `android.permission.INTERNET` y el permiso generado `com.yecoworks.squidinkpulse.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`;
- los permisos deben revisarse antes de una candidata. Este corte registra su presencia, pero no elimina dependencias ni atribuye su origen sin una inspección separada;
- el SDK local no incluye Emulator/AVD; el bootstrap se verificó en el teléfono físico descrito arriba.

### Corrección de resolución y encuadre de `MainMenu`

La causa del contenido comprimido era `OptionsMenuManager`: durante `Start` aplicaba controles de resolución de escritorio aunque el panel de opciones estuviera cerrado. Android informó la pantalla física como `1220x2712`, y esa medida portrait se guardó mediante `Screen.SetResolution` sobre una ventana landscape de `2712x1220`.

La corrección:

- no ofrece ni aplica selección de resolución o fullscreen de escritorio en plataformas móviles;
- normaliza la resolución móvil existente al formato landscape y usa `FullScreenWindow`, por lo que también recupera instalaciones que ya guardaron la medida portrait incorrecta;
- escala el canvas raíz de `MainMenu` por altura en móvil para mantener visibles los controles verticales;
- extiende sólo el fondo decorativo al rect completo para evitar bandas laterales sin alterar la escala segura de botones y logo.

La actualización se instaló con `adb install -r` sobre los PlayerPrefs defectuosos. Tras abrirla, los valores internos de Unity cambiaron de `1220x2712`/`ExclusiveFullScreen` a `2712x1220`/`FullScreenWindow`. La captura final ocupa toda la pantalla sin las bandas observadas, `Opciones` abre y regresa mediante touch físico, el proceso permanece activo y logcat filtrado no registra errores Unity ni AndroidRuntime. La suite EditMode aprobó `154/154`, la composición canónica de escenas fue válida y el smoke Windows a 1280x720 permaneció receptivo sin excepciones compartidas.

### Checkpoint de preferencias al abandonar primer plano

La persistencia permanente JSON ya escribe de forma inmediata y atómica; por ello el checkpoint de ciclo de vida no vuelve a serializar perfil, records, catálogo ni leaderboard, y tampoco captura estado transitorio de una run. Las preferencias conocidas conservan su confirmación inmediata y cualquier cambio que permanezca pendiente se vacía una sola vez al recibir pausa o pérdida de foco en un player móvil.

La APK actualizada se instaló sobre los datos existentes y se comprobó en el mismo POCO X6 5G con este recorrido: modificar el volumen, volver a `MainMenu`, enviar la aplicación a segundo plano, terminar el proceso y reabrirla. `MasterVolume` reapareció con el valor modificado (`0.3565277`); `player-profile.json`, `player-records.json` y `unlockables-catalog.json` conservaron sus SHA-256 previos, mientras `local-leaderboard.json` siguió ausente por su creación diferida. El proceso y la actividad regresaron al primer plano y logcat filtrado registró cero errores `Unity:E` y `AndroidRuntime:E`.

La validación complementaria aprobó `159/159` pruebas EditMode, auditor Android con `0` errores, `0` advertencias y `10` datos, composición canónica válida y APK de 514.213.206 bytes. El build Windows compartido permaneció receptivo a 1280x720 y no registró excepciones.

## Criterio de cierre

La puerta de `mobile/01-android-bootstrap` está satisfecha: el APK reproducible se instaló y abrió `MainMenu` dos veces en Android sin crash bloqueante. El defecto puntual de resolución y encuadre observado durante ese bootstrap también está corregido y validado.

La puerta de `mobile/02-mobile-persistence` también está satisfecha: instalación limpia, actualización sobre datos existentes, recuperación/migración y suspensión con reapertura conservan el contrato local. La integración touch y el primer encuadre extra-wide se documentan a continuación; todavía quedan una run física completa, adaptación integral de opciones y Back/reanudación, aislamiento de feria, orientación landscape opuesta y rendimiento sostenido.

## Evidencia de integración touch y encuadre extra-wide

El 8 de agosto de 2026 se generó e instaló mediante `adb install -r` una APK Development con controles touch integrados en ambas zonas. La primera validación física encontró dos defectos: el avance horizontal esperaba el primer objetivo vertical y los Canvas de gameplay reducían su alto lógico en 20:9, recortando HUD y Game Over. Ambos se corrigieron antes de aceptar el gate.

- `PlayerMovementFramePolicy` mantiene el autoscroll aunque no exista mouse o dedo; el input sólo guía Y.
- HUD y TouchControls viven bajo un `SafeAreaRoot` gobernado por `Screen.safeArea`.
- HUD, pausa, Game Over, tienda y cómic usan referencia 1920x1080 con match por altura.
- El botón de habilidad conserva el nombre de producto `INK-PULSO`.
- Suite final previa al último artefacto: EditMode `243/243` y PlayMode `2/2`; el segundo PlayMode usa `InputSystemUIInputModule`, touchscreen virtual y cambio Epipelágica→Abisopelágica.
- APK final con ajuste textual: 514.308.880 bytes, SHA-256 `A073C7629FB4FD7D1B8CA0B8FBCBF7BEA4201EDDC362FD2D4DF01FD70CC75594`, instalación `Success`, proceso activo y cero coincidencias críticas de crash.

La captura posterior mostró score, gadgets, barra Ink-Pulse y cuatro botones completos dentro del frame 2712x1220; `INK-PULSO` cabe sin recorte. Esta evidencia no sustituye completar una run en ambas orientaciones landscape ni la medición sostenida de rendimiento.

## Evidencia de pulido responsive de menús

Una actualización posterior corrigió tres defectos acotados observados en el mismo POCO: los Canvas del cómic `¿Cómo Jugar?` ahora preservan el alto 1080; `LetreroMejoras` y `LetreroAspectos` mantienen proporción 3:1 y anclaje derecho; el `Character` decorativo del Main Menu fija su pivote 0.75 sobre el borde derecho para mostrar sólo sus tres cuartas partes útiles.

- Suite EditMode: `246/246`; composición canónica válida.
- APK Development: 514.308.880 bytes, SHA-256 `FC06FA2ED77B2CD2ED8982FE32E42BBBCF6568CB29FD5162AEDE0A3CA040B2DF`.
- Instalación actualizada y arranque correctos; proceso activo y cero coincidencias críticas de crash.
- Revisión física aceptada para los tres ajustes. Las bandas grises laterales del fondo 16:9 de `ShopMenu` permanecen como deuda visual independiente.

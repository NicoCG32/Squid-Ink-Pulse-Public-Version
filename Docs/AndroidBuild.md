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

## Criterio de cierre

La puerta de `mobile/01-android-bootstrap` está satisfecha: el APK reproducible se instaló y abrió `MainMenu` dos veces en Android sin crash bloqueante. El defecto puntual de resolución y encuadre observado durante ese bootstrap también está corregido y validado. Esto no acepta todavía gameplay mediante touch, adaptación integral de opciones y safe area, ciclo de vida, aislamiento de feria ni rendimiento sostenido; esas puertas pertenecen a las ramas posteriores del port.

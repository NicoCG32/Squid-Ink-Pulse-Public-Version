# Build e instalación Android

## Estado

El proyecto dispone de comandos para generar un APK Development o un APK sin Development. La compilación, inspección estática, instalación y apertura de `MainMenu` están verificadas en Android ARM64 mediante ADB. Los canales y artefactos publicados se describen en [Distribución](Distribution.md).

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

El builder Development es propietario de esta carpeta y la limpia antes de cada ejecución:

```text
Build/AndroidDevelopment/SquidInkPulse-development.apk
```

`Build/` y `*.apk` están ignorados por Git. No se deben agregar binarios Android al repositorio.

Este comando solicita `BuildOptions.Development` y `AllowDebugging`; no genera una candidata con firma de distribución. El postprocesador de feria se limita a Windows y no genera guías `.exe` ni scripts junto al APK.

### APK sin Development

Usar `Tools > Squid Ink Pulse > Build Android Performance APK (No Development)` o el método batch `AndroidDevelopmentBuilder.BuildAndroidPerformanceApk`, manteniendo `-buildTarget Android`. Genera `Build/AndroidPerformance/SquidInkPulse-performance.apk` con `BuildOptions.None` y rechaza una salida no vacía para conservar los artefactos previos. Comparte las cuatro escenas y la validación de composición del builder Development.

Esta modalidad permite medir y distribuir una beta sin el coste de Development; no configura por sí sola firma de distribución. Registrar commit, versionCode, certificado y checksum antes de publicar. Véase [comparación de rendimiento](AndroidPerformance.md) y [canal Android beta](Distribution.md#android-beta).

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

### Checkpoint de preferencias al abandonar primer plano

La persistencia permanente JSON ya escribe de forma inmediata y atómica; por ello el checkpoint de ciclo de vida no vuelve a serializar perfil, records, catálogo ni leaderboard, y tampoco captura estado transitorio de una run. Las preferencias conocidas conservan su confirmación inmediata y cualquier cambio que permanezca pendiente se vacía una sola vez al recibir pausa o pérdida de foco en un player móvil.

La APK actualizada se instaló sobre los datos existentes y se comprobó en el mismo POCO X6 5G con este recorrido: modificar el volumen, volver a `MainMenu`, enviar la aplicación a segundo plano, terminar el proceso y reabrirla. `MasterVolume` reapareció con el valor modificado (`0.3565277`); `player-profile.json`, `player-records.json` y `unlockables-catalog.json` conservaron sus SHA-256 previos, mientras `local-leaderboard.json` siguió ausente por su creación diferida. El proceso y la actividad regresaron al primer plano y logcat filtrado registró cero errores `Unity:E` y `AndroidRuntime:E`.

La validación complementaria aprobó `159/159` pruebas EditMode, auditor Android con `0` errores, `0` advertencias y `10` datos, composición canónica válida y APK de 514.213.206 bytes. El build Windows compartido permaneció receptivo a 1280x720 y no registró excepciones.

## Alcance de la beta y comprobaciones

El APK generado por esta guía es Development y permite depuración. La beta publicada se encuentra en [GitHub Releases](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases); consulta las notas y el checksum de su archivo para identificar la versión que instalas.

La adaptación móvil comprende controles táctiles, menús, opciones y área segura. Antes de distribuir una versión final, comprueba:

- Menú, tutorial, tienda, pausa y regreso al juego en el dispositivo objetivo.
- Una partida completa por ambas zonas, Game Over y reintento.
- Conservación del perfil y del volumen después de cerrar, reabrir y actualizar.
- Botón Atrás, suspensión, reanudación y ambas orientaciones admitidas.
- Legibilidad con distintas relaciones de aspecto, recortes de pantalla y rendimiento sostenido.

Las pruebas de Editor y las capturas automáticas complementan la prueba física. El alcance y los requisitos del port se describen en [MobilePort](MobilePort.md); las comprobaciones de escritorio se mantienen en [Testing](Testing.md).

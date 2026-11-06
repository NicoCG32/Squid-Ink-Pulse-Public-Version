# Build e instalación Android

## Modalidades

El proyecto ofrece APK Development para depuración y APK sin Development para evaluación de rendimiento y distribución beta. Los canales publicados están en [Distribución](Distribution.md).

## Requisitos locales

- Unity `6000.3.11f1`;
- Android Build Support de esa misma instalación;
- SDK, NDK y OpenJDK instalados mediante Unity Hub;
- las cuatro escenas obligatorias habilitadas en Build Settings;
- para instalar, un teléfono con depuración USB autorizada o un emulador visible en `adb devices`.

Las rutas locales del SDK y JDK no se guardan en el proyecto.

## Generar el APK

### Iconos de Android y Windows

La configuración incluye el icono del calamar para Windows y los iconos Android legacy, redondo y adaptable. El adaptable usa un fondo opaco y una capa frontal transparente con margen para los recortes del launcher; sus originales están en `branding/iconos/android-adaptive-*.svg` y los PNG de build en `Assets/Content/Art/UI/Brand/`.

Para reasignarlos, usar `Squid Ink-Pulse > Identity > Apply Android and Windows icons` o el método batch `ProjectBranding.ApplyPlayerIcons`. Este comando sólo modifica los iconos de Player Settings. Recompilar el APK y el ejecutable Windows para que incluyan el cambio; una build ya generada no se actualiza al editar el proyecto.

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

Esta modalidad permite medir y distribuir una beta sin el coste de Development; no configura por sí sola firma de distribución. Registrar commit, versionCode, certificado y checksum antes de publicar. Véase [comparación de rendimiento](AndroidPerformance.md) y [canal Android beta](Distribution.md#generar-android).

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

Para comprobarlo, cambiar el volumen, enviar la aplicación a segundo plano, cerrar el proceso y reabrir. Revisar el valor recuperado y la conservación de perfil, récords y catálogo.

## Alcance de la beta y comprobaciones

El comando Development permite depuración; el comando Performance utiliza `BuildOptions.None`. La beta publicada se encuentra en [GitHub Releases](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases); consulta las notas y el checksum de su archivo para identificar la versión que instalas.

La adaptación móvil comprende controles táctiles, menús, opciones y área segura. Antes de distribuir una versión final, comprueba:

- Menú, tutorial, tienda, pausa y regreso al juego en el dispositivo objetivo.
- Una partida completa por ambas zonas, Game Over y reintento.
- Conservación del perfil y del volumen después de cerrar, reabrir y actualizar.
- Botón Atrás, suspensión, reanudación y ambas orientaciones admitidas.
- Legibilidad con distintas relaciones de aspecto, recortes de pantalla y rendimiento sostenido.

Las pruebas de Editor y las capturas automáticas complementan la prueba física. El alcance y los requisitos del port se describen en [MobilePort](MobilePort.md); las comprobaciones de escritorio se mantienen en [Testing](Testing.md).

## Juego local y permisos

El player Android omite la integración de feria. La build normal sin Development se empaqueta sin permiso de Internet y se comprueba también con modo avión y Wi-Fi apagado. El postproceso deja intacta la variante Development para depuración. Consulta [Android sin conexión y modo feria](AndroidOffline.md) para revisar los permisos del APK final y el alcance de la comprobación por plataforma.

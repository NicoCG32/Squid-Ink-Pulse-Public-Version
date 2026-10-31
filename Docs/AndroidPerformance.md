# Rendimiento y medición en Android

## Objetivo de presentación

`AndroidFramePacing` solicita 60 FPS antes de cargar la primera escena de un player Android. Unity y el sistema ajustan la presentación a las capacidades y el refresco del dispositivo: solicitar 60 no garantiza sostenerlos. El inicializador no modifica Windows, Editor, resolución, URP, física, balance ni preferencias guardadas.

El valor implícito de Unity en Android puede limitar la presentación a 30 FPS incluso cuando sobra capacidad de renderizado. La referencia oficial de [Application.targetFrameRate](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/application/targetframerate) explica el comportamiento móvil y el ajuste a divisores del refresco.

## Player de medición

Para construir un APK sin Development ni AllowDebugging:

```text
Tools > Squid Ink Pulse > Build Android Performance APK (No Development)
```

En batch, desde la raíz del proyecto:

```powershell
& $unity -batchmode -quit -buildTarget Android -projectPath $PWD `
  -executeMethod AndroidDevelopmentBuilder.BuildAndroidPerformanceApk `
  -logFile TestResults/android-performance-build.log
```

La salida es `Build/AndroidPerformance/SquidInkPulse-performance.apk`. El builder rechaza una salida no vacía: conservarla antes de repetir. Usa las cuatro escenas canónicas y el mismo prebuild de composición del APK Development. Sin una configuración de firma de distribución, continúa siendo un artefacto local firmado con el certificado de desarrollo; no equivale a una candidata publicada.

Instalar mediante `adb install -r`, conservando los datos. Registrar el commit, checksum y las opciones de build del artefacto. La medición externa no necesita que el player sea debuggable; para atribuir costes de scripts con Unity Profiler, utilizar un APK Development de la misma revisión y registrar esa diferencia.

## Comparación de 30 y 60 FPS con el mismo APK

La bandera diagnóstica `--sip-perf-30fps` permite usar temporalmente un objetivo de 30 para comparar el mismo player, calidad y contenido. No se escribe en PlayerPrefs ni en el perfil. Android entrega la bandera en el extra `unity` de la actividad:

```powershell
& $adb -d shell am force-stop com.yecoworks.squidinkpulse
& $adb -d shell am start -W `
  -n com.yecoworks.squidinkpulse/com.unity3d.player.UnityPlayerGameActivity `
  -e unity '--sip-perf-30fps'
```

Para volver al objetivo normal de 60, terminar el proceso y lanzar sin el extra:

```powershell
& $adb -d shell am force-stop com.yecoworks.squidinkpulse
& $adb -d shell am start -W `
  -n com.yecoworks.squidinkpulse/com.unity3d.player.UnityPlayerGameActivity
```

Cerrar el proceso sólo entre recorridos. Esperar a que finalice la carga y estabilizar el escenario antes de medir. El log `[AndroidFramePacing]` identifica objetivo, build GUID, calidad y si el player es Development.

## Captura reproducible

`Tools/Android/Measure-FramePacing.ps1` requiere PowerShell 7, ADB y un único dispositivo autorizado. Desde la raíz:

```powershell
$revision = git rev-parse --short HEAD
./Tools/Android/Measure-FramePacing.ps1 -AdbPath $adb `
  -Label menu-60-pase1 -SourceCommit $revision `
  -ApkPath Build/AndroidPerformance/SquidInkPulse-performance.apk `
  -Configuration Performance -Seconds 15
```

La herramienta:

- verifica que el SHA-256 del APK instalado coincide con el archivo suministrado;
- exige que el juego tenga foco antes y después de la muestra;
- recoge estadísticas de SurfaceFlinger y conserva sólo la superficie del juego;
- guarda FPS presentados, voto de frecuencia, histograma de intervalos y percentiles por bucket;
- registra memoria PSS y condiciones de batería/estado térmico antes y después;
- captura imágenes fuera de la ventana medida para comprobar el escenario.

Los resultados se escriben en `TestResults/MobilePerformance/`, fuera de Assets e ignorados por Git. Cada etiqueta es única; no sobrescribe un informe existente. Las capturas y los datos son locales, sin envío a un servicio externo. No se borra ni modifica progreso.

El escenario es una etiqueta del operador, no una detección automática de escena. Revisar ambas imágenes y separar muestras con cargas, cómics, pausa o Game Over. Repetir al menos dos veces por condición, manteniendo dispositivo, APK, refresco, calidad y recorrido comparables. Registrar por separado una sesión sostenida; una muestra corta no demuestra resistencia al calentamiento.

## Interpretar las métricas

SurfaceFlinger mide intervalos entre imágenes presentadas, según el procedimiento de [Android para sesiones lentas](https://developer.android.com/topic/performance/issues/slow-session). Los percentiles del script son **buckets cuantizados**, no tiempos exactos de ejecución. Un bucket de 17 ms es normal al presentar cerca de 60 FPS; no debe contarse automáticamente como un incumplimiento de 16,67 ms.

Los tiempos de CPU/GPU por frame y las asignaciones de GC quedan `null` porque esta herramienta no los atribuye. Usar Unity Profiler o una traza de sistema para investigar esas causas. PSS es memoria del proceso, no el tamaño del APK ni una medición exclusiva de VRAM. El estado térmico global no sustituye una captura sostenida ni identifica por sí solo una causa de throttling.

Comparar primero el ritmo de presentación. Si, con un objetivo de 60, persisten caídas o picos, medir CPU/GPU, cargas y memoria antes de cambiar resolución, HDR, texturas, audio o spawns.

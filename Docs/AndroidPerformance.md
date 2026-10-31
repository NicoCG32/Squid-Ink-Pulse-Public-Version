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

La salida es `Build/AndroidPerformance/SquidInkPulse-performance.apk`. El builder rechaza una salida no vacía: conservarla antes de repetir. Usa las cuatro escenas canónicas y el mismo prebuild de composición del APK Development. Sin una configuración de firma de distribución, continúa firmado con el certificado de desarrollo; puede distribuirse como beta tras su validación, pero no equivale a una candidata estable. Consultar [canales y procedencia](Distribution.md#android-beta).

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

`averageFPS` puede reflejar esa cuantización: 1000/16 da 62,5 aunque el player esté presentando aproximadamente 60 FPS. Se conserva el valor reportado y se añade una estimación por número de intervalos/tiempo de pared; esta última incluye latencia de comandos. Interpretar ambos junto con el voto de frecuencia y el histograma, sin afirmar precisión submilisegundo.

Si la diferencia relativa entre ambos supera el 15%, el informe marca `HistogramWallTimeReviewRequired`. Es una alerta heurística de comparabilidad, no un presupuesto de rendimiento. Revisar cambios de escena, reinicios, alcance temporal y posibles intervalos no representados; contrastar con una traza independiente antes de afirmar FPS sostenidos o atribuir duración a los tirones. Un p95/p99 pequeño no resuelve por sí solo esa discrepancia.

Los tiempos de CPU/GPU por frame y las asignaciones de GC quedan `null` porque esta herramienta no los atribuye. Usar Unity Profiler o una traza de sistema para investigar esas causas. PSS es memoria del proceso, no el tamaño del APK ni una medición exclusiva de VRAM. El estado térmico global no sustituye una captura sostenida ni identifica por sí solo una causa de throttling.

Cuando Android permite leer `/proc/<pid>/stat`, también se registra el tiempo de CPU del proceso sumando sus contadores de usuario y kernel, con el reloj `CLK_TCK` del dispositivo. Se comprueban PID y tiempo de inicio para descartar reinicios/reutilización. `ProcessCpuOneCorePercent` usa 100% por núcleo y puede superar 100%; incluye todos los hilos del juego y comandos de instrumentación en los extremos. No mide CPU por frame ni identifica qué hilo o función limita. Si faltan permisos, muestras o reloj válido, los valores permanecen `null`. Los [campos de `/proc` se documentan en el kernel](https://www.kernel.org/doc/html/latest/filesystems/proc.html).

Comparar primero el ritmo de presentación. Si, con un objetivo de 60, persisten caídas o picos, medir CPU/GPU, cargas y memoria antes de cambiar resolución, HDR, texturas, audio o spawns.

## Sesiones por ventanas

`Measure-PerformanceSession.ps1` repite la captura sin enviar controles ni modificar el progreso. El juego debe estar abierto y tener foco. Puede jugarse normalmente durante la sesión:

```powershell
./Tools/Android/Measure-PerformanceSession.ps1 -AdbPath $adb `
  -Label run-tablet-pase1 -SourceCommit $revision `
  -ApkPath Build/AndroidPerformance/SquidInkPulse-performance.apk `
  -Windows 12 -SecondsPerWindow 15
```

Usar el commit **del APK**, que puede diferir del checkout de las herramientas. La salida queda en `TestResults/MobilePerformance/<etiqueta>/`. `session.json` registra las ventanas completadas, los hashes de las herramientas, su duración y cualquier interrupción; los informes individuales verifican el APK instalado. Una carpeta existente se conserva y obliga a elegir otra etiqueta.

Cada ventana empieza como `Unreviewed`. Revisar sus dos capturas, anotar zona/overlay y excluir transiciones o derrotas de las comparaciones de gameplay. Hay intervalos de instrumentación entre ventanas: la duración total incluye imágenes y comandos y no constituye una traza continua. No se calcula un FPS global que mezcle menú, run y Game Over. Una sesión completa sin revisar tampoco certifica rendimiento sostenido.

## Recursos incluidos en la build

`AndroidResourceAuditExporter` lee el [último Build Report guardado por Unity](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/build/reporting/buildreport/getlatestreport) y las opciones actuales de los importadores, sin reimportar ni modificar assets. Requiere que el último build sea Android correcto y que su APK todavía exista. Desde el menú: `Tools > Squid Ink Pulse > Export Last Android Resource Audit`. En batch:

```powershell
& $unity -batchmode -quit -buildTarget Android -projectPath $PWD `
  -executeMethod AndroidResourceAuditExporter.ExportLatest `
  --sip-audit-label android-baseline `
  --sip-audit-source-commit $revision `
  --sip-audit-build-guid $buildGuid --sip-audit-apk-sha256 $apkSha256 `
  -logFile TestResults/android-resource-audit.log
```

Los parámetros de GUID y checksum permiten contrastar el reporte con el artefacto esperado. El commit debe tomarse de la procedencia del APK; es una etiqueta del operador y, si se omite, queda `unrecorded` en lugar de asumir el HEAD de las herramientas. La salida `TestResults/AndroidResourceAudit/<etiqueta>.json` conserva el GUID, SHA-256 del APK, tamaños de [contenido serializado por asset](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/build/reporting/packedassets), totales por carpeta/tipo y opciones Android de textura/audio. También registra los overrides globales del Editor y observa el formato de tres texturas dominantes.

El tamaño serializado no equivale a APK comprimida ni a RAM/VRAM residente. Los niveles de escena, librerías nativas y otros archivos no están completamente atribuidos por esta tabla. Los importadores y overrides son los del checkout/Editor al capturar, no una instantánea histórica del build; los formatos observados en Editor tampoco prueban el formato residente en el teléfono. Comprobar estas diferencias antes de cambiar compresión, resolución, atlases o audio. El informe prioriza candidatos; no demuestra qué recurso causó un tirón.

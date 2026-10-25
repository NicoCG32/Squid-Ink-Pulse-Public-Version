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

### APK Development de menú/opciones y HUD 16:10 — 12 de octubre de 2026

Se generó desde `mobile/06-main-menu-shop`, código `a5a031d`, conservando fuera de los commits la serialización previa de `TimeManager.asset`. No se atribuye el artefacto a un checkout totalmente limpio.

- `Succeeded`, 245,96 s, 514.323.277 bytes (490,50 MiB), 0 errores y 2 advertencias.
- SHA-256: `4C52CB226A61D613D334A4F16CBEF0B28D1EB574605B301769C8AA020C5FCF89`.
- 271/271 EditMode, 5/5 PlayMode, sin omitidas; composición aprobada. Matriz de catorce previews: 112 bounds dentro del área segura, cero fallos; 18 intersecciones de contenedores y 42 observaciones TMP informativas.
- Instalación `adb install -r` con `Success` en Redmi Pad Pro, Android 16/API 36, 2560×1600 landscape. Menú, Cómo Jugar y contador visibles completos; opciones sólo muestran volumen/regreso. El valor de volumen cambiado a `0.4805011` sobrevivió a cierre forzado, reapertura y apertura de opciones. Se restituyó aproximadamente el nivel previo de 0,75 al finalizar la prueba.
- Se inició una run, se observó Game Over, se ejecutó retry y se capturó Epipelágica pausada con gadgets, contador, puntuación y controles completos. El usuario confirmó lectura y comodidad del menú, volumen y HUD en la tablet. Abisopelágica queda cubierta por previews y PlayMode; no se afirma una run física completa ni una visita a esa zona en este pase.
- Log filtrado del proceso sin excepciones críticas observadas. Windows compiló con `BuildOptions.None`, 0 errores/2 advertencias y smoke oculto de arranque de 15 s vivo/responsivo. No se atribuye al smoke una run interactiva ni una medida efectiva de ventana; las opciones guardadas pueden reaplicar fullscreen.

Este APK de desarrollo está instalado para continuar las pruebas; la beta publicada en GitHub sigue siendo el artefacto anterior. Persisten los cortes de menús/safe area, overlays, Back/ciclo de vida, orientación opuesta y rendimiento sostenido.

### APK Development de MainMenu seguro y cómics — 15 de octubre de 2026

Fuente `bad3bd1` en `mobile/06-main-menu-shop`, más la serialización entrante de `TimeManager.asset` preservada fuera de commits. Es un APK Development de revisión, no una candidata final ni una actualización de la beta publicada.

- Resultado `Succeeded`, 262,18 s, 514.338.819 bytes, 0 errores/2 advertencias; composición canónica validada.
- SHA-256: `20F8B7EC56B53B0EBC04296480B05473D8A27A78E4F88ED2AC393EACAA311905`.
- Suite final: 298/298 EditMode y 6/6 PlayMode, sin omitidas. Matriz MainMenu: 6 capturas/36 bounds dentro; tutorial: 18 capturas/4.428 glifos dentro y Next dentro en todos los formatos, incluidos cutouts simulados a ambos lados.
- Instalación `adb install -r` con `Success` en Redmi Pad Pro. Se capturaron las tres páginas completas de Cómo Jugar y su regreso al menú mediante taps ADB; desaparece el recorte derecho reproducido en la build anterior. El dimmer sigue cubriendo el frame y no se deforman las viñetas.
- El pase físico del menú comprobó opciones, tutorial, tienda/regreso, cómic inicial y entrada a Epipelágica/pausa. No incluye compras, visita física a Abisopelágica, orientación opuesta ni una run final completa. La validación actual no reemplaza la aceptación ergonómica previa del usuario ni afirma una nueva aceptación manual del arte del tutorial.
- Log del proceso del APK corregido sin coincidencias críticas de excepción observadas.
- Regresión Windows de la misma fuente: `BuildOptions.None`, 26,48 s, 0 errores/2 advertencias y composición aprobada; smoke oculto de 15 s vivo y responsivo, sin excepciones observadas. No certifica una run interactiva ni el tamaño efectivo de la ventana, que puede depender de las preferencias guardadas.

Los controles desktop de opciones permanecen ocultos. El layout seguro de ese panel, los iconos de input del arte tutorial y la adaptación completa de tienda continúan como trabajo posterior; se conserva la UI Windows de autoría.

### Opciones seguras e instrucciones táctiles — 18 de octubre de 2026

Fuente `a3e5b73` en `mobile/06-main-menu-shop`, más la serialización entrante de `TimeManager.asset` preservada fuera de commits. Este corte conserva la UI de escritorio y adapta el prefab compartido de opciones: volumen y Volver viven bajo área segura con escala uniforme, mientras el fondo bloquea toques sobre todo el viewport. El Canvas anidado de MainMenu sigue las medidas del viewport móvil también después del escalado del Canvas raíz.

Los textos móviles de Cómo Jugar indican tocar `INK-PULSO` y arrastrar en pantalla para moverse; las ilustraciones existentes y los textos desktop se conservan. Las variantes vacías mantienen el texto de autoría.

- Suite final de UI: 307/307 EditMode y 6/6 PlayMode, sin omitidas. El recorrido touch comprueba además bloqueo del fondo y cambio del slider mediante el módulo UI real; el test aísla el listener de persistencia para no modificar preferencias del Editor.
- Matriz de opciones: cuatro escenas por seis formatos, 24 capturas/96 rectángulos/0 fallos, fondo completo y posiciones del handle en 0/0,5/1 dentro del área segura. Incluye tablet 16:10 y cutouts simulados a ambos lados.
- Matriz tutorial con el texto móvil: 18 capturas/4.242 glifos/0 fuera; Next dentro en todos los formatos.
- El primer prebuild detectó que el validador contaba el área segura de opciones dentro del contrato del HUD. La comprobación ahora se limita a `HudRoot`; la composición canónica de las cuatro escenas vuelve a pasar sin omitir el contrato independiente de opciones.
- APK Development: `Succeeded`, 303,29 s, 514.340.948 bytes, 0 errores/2 advertencias; SHA-256 `C845D1CAA10EF02D12C71C2AE67BAD1B7D616B1E02CE7B26BB18296CE9178CD9`. Instalación actualizada `-r` con `Success` en Redmi Pad Pro.
- Pase físico mediante taps/arrastre ADB: opciones desde MainMenu, ShopMenu y pausa de Epipelágica; slider operativo, fondo bloqueando tienda/Reanudar situados detrás y Volver conservando la pausa. Las tres páginas muestran textos completos y el tercer Next regresa al menú. Se dejó la tablet en MainMenu; log por PID sin coincidencias críticas.
- Regresión Windows de la misma fuente: `BuildOptions.None`, 31,12 s, 0 errores/2 advertencias y composición aprobada; smoke oculto de 15 s vivo/responsivo, sin excepciones observadas. Las preferencias guardadas reaplicaron fullscreen y Unity usó fallback de resolución; el smoke no certifica una run interactiva ni una ventana efectiva de 1280×720.

La matriz automática incluye Abisopelágica; este pase físico no la visitó ni comprobó compras, Back de Android, orientación opuesta, una run completa o rendimiento sostenido. El APK instalado es de revisión local y no actualiza la beta publicada.

### Tienda permanente segura — 18 de octubre de 2026

Fuente `adc08c6` en `mobile/06-main-menu-shop`, más el `TimeManager.asset` entrante preservado fuera de commits. `ShopSafeArea/ShopContent` protege controles, catálogo, detalle y contador con encaje uniforme 1920×1080. El fondo cubre el viewport móvil mediante overscan proporcional; los nombres de producto usan auto size móvil. Se conservan catálogo, economía, eventos y geometría de escritorio.

- Suite final: 312/312 EditMode y 7/7 PlayMode, sin omitidas.
- Matriz final: ocho formatos, 104 hitboxes, 96 selecciones y 6.632 glifos, 0 fallos de cobertura, safe area u overflow. Incluye 19,5:9, 20:9, cutouts a ambos lados y tablet 16:10.
- El recorrido PlayMode usa touch/UI real y un perfil en memoria con guardado transitorio de Editor: compra de mejora y skin, equipar/quitar sin doble descuento, paginación, rechazo por falta de saldo y exclusión de una entrada futura sin sprite. No modifica los fondos ni compras del perfil del usuario.
- Cancel móvil usa la acción del módulo UI de ShopMenu: cierra opciones primero y vuelve al menú desde tienda. Este cambio no certifica el Back global de otras escenas ni el ciclo de vida completo.
- La primera revisión física mostró que el resultado `SIN CAMARONES` no tenía un TMP conectado. Se añadió `EstadoCompraMovil` dentro del contenido seguro, oculto en desktop, y se verificó el texto visible en PlayMode y la tablet.
- APK final Development: `Succeeded`, 81,28 s, 514.351.891 bytes, 0 errores/2 advertencias; composición validada. SHA-256 `BDF9F8028EC059D2B20902ABB649C0F9674689A96D04A636A6D8B3E5493A7E5C`. Instalación actualizada `-r` con `Success` en Redmi Pad Pro.
- Pase físico: fondo sin bandas, nombre largo completo, selección de mejora/skins, navegación entre ambas páginas y precios, rechazo visible por falta de saldo y contador conservado en 7. Back de Android cierra opciones y después regresa de tienda a MainMenu; log por PID sin coincidencias críticas. Compras exitosas y equipamiento se verificaron en PlayMode con perfil transitorio. La tablet quedó en MainMenu.
- Regresión Windows de la misma fuente: `BuildOptions.None`, 22,42 s, 0 errores/2 advertencias y composición aprobada; smoke oculto de 15 s vivo/responsivo y sin excepciones observadas. Las preferencias guardadas provocaron fallback de fullscreen; no certifica una run interactiva ni una medida efectiva de ventana. Estos artefactos de revisión no actualizan la beta publicada.

### Comprobación integral de menú y tienda — 19 de octubre de 2026

Se amplió la navegación PlayMode hasta regresar de tienda, tocar Jugar, continuar el cómic inicial y entrar a Epipelágica con HUD activo y tiempo normal. Un caso adicional comprueba en ShopMenu la presentación desktop, resolución/fullscreen visibles y ausencia de Cancel móvil al pulsar Escape. Los botones se ejercen mediante el módulo UI real, sin mouse ni llamadas directas a OnClick; no se modifican preferencias de display.

La suite completa aprueba 312/312 EditMode y 8/8 PlayMode, sin omitidas, y el validador de composición confirma escenas/prefabs canónicos. Los contratos, matrices y revisión física se complementan según la [cobertura de pruebas](Testing.md). La prueba de navegación espera la primera actualización de Canvas tras cargar tienda antes de enviar un toque; activar la escena no garantiza que sus gráficos ya estén registrados para raycast.

Este cierre sólo modifica tests y documentación. Código incluido, escenas, prefabs, assets y configuración de producto conservan la fuente `adc08c6` de los artefactos anteriores; `TimeManager.asset` sigue siendo la modificación local preservada con la que se construyeron. Se reutilizan el APK de hash `BDF9F8028EC059D2B20902ABB649C0F9674689A96D04A636A6D8B3E5493A7E5C` instalado en tablet y la build/smoke Windows del 18 de octubre, sin atribuirles una recompilación ni revisión física nuevas. La beta publicada sigue siendo la del 9 de octubre. Overlays de run, Back global, orientación opuesta, ciclo de vida y rendimiento mantienen sus validaciones independientes.

### Pausa y Game Over seguros — 23 de octubre de 2026

Fuente `1ede998` en `mobile/07-run-overlays`, más el `TimeManager.asset` local preservado. Ambos prefabs comparten el adaptador de safe area y el encaje uniforme 1920×1080 ya existentes: botones, decoración y puntajes quedan dentro del contenido seguro; los fondos siguen cubriendo el viewport completo. Se conservan posiciones locales de animación, cálculo de score y presentación desktop.

- Suite final: 317/317 EditMode y 10/10 PlayMode, sin omitidas. Los casos nuevos completan diez pausas/reanudaciones por zona, opciones conservando pausa, regreso al menú y tres retries desde Abisopelágica hacia Epipelágica con recuperación de steering. La derrota automatizada usa la frontera real de sesión y espera el cómic/animación; perfil transitorio y spawners aislados, sin afirmar una prueba de física ni una run completa.
- Matriz de ambas zonas: 32 capturas, 112 rectángulos de controles/textos y 816 glifos, incluidos puntajes hasta `long.MaxValue`, sin fallos de safe area o cobertura. Complementa routing y revisión física; no certifica ergonomía ni animación por sí sola.
- APK Development: `Succeeded`, 105,33 s, 514.351.912 bytes, 0 errores/2 advertencias y composición validada. SHA-256 `8E1ED73C9F5CE52E4F117B97713B26E43C3C825CD41F2AD75161FB53E780E8E5`. Instalación actualizada `-r` con `Success` en Redmi Pad Pro.
- Pase físico en Epipelágica: cómic inicial, derrota natural, Game Over completo con puntajes y Reintentar; nueva run pausada, opciones/Volver conservando puntaje, Reanudar y una segunda pausa correctos. Regreso a MainMenu desde pausa y, tras otra derrota natural, desde Game Over. Log del proceso sin coincidencias críticas; tablet dejada en MainMenu. No se modificó el volumen ni se acreditaron fondos de prueba al perfil real.
- Windows de la misma fuente: `BuildOptions.None`, `Succeeded`, 27,60 s, 0 errores/2 advertencias y composición validada. Smoke oculto de 15 s vivo/responsivo, sin excepciones críticas observadas. Las preferencias guardadas provocaron fallback de fullscreen; no certifica una run interactiva ni una ventana efectiva de 1280×720.

Los artefactos de este corte quedan preservados bajo `Build/AndroidReview/1ede998` y `Build/WindowsReview/1ede998`; los anteriores de `adc08c6` también se conservan. Evidencia local ignorada `TestResults/overlays-20261023-*` y galería `RunMenuPreview`. No se afirma visita física a Abisopelágica, orientación opuesta, Back global, ciclo de vida ni rendimiento sostenido. El APK sigue siendo de revisión local; la beta publicada no se actualiza con este pase.

### Tienda de run táctil y segura — 23–24 de octubre de 2026

Fuente de presentación `230f7eb` y de layout/wiring/pruebas `698d49d`. La oferta, precio, botón, aviso sin saldo y contador quedan dentro del contenido seguro; el fondo bloquea el viewport completo. Android muestra `TOCA PARA COMPRAR` y `TIEMPO`, mientras Windows conserva las tres variantes del atajo B y la tipografía original. El aviso sin saldo permanece visible al destacar o presionar el botón. Los contadores se enlazan en ambos GameRoot y también en las referencias propias de las dos escenas.

- EditMode: `323/323`; PlayMode: dos pases completos consecutivos `12/12`, sin omitidas. Compra suficiente/insuficiente, expiración en tiempo real con gameplay congelado, descuento único, recuperación del steering y rechazo de Ink-Pulse en el cierre y el frame siguiente; un tap posterior deliberado funciona. Compra B aprobada. Perfil transitorio y precio/duración de prueba; no certifica el spawn/contacto de DealerFish ni sus cómics.
- Matriz Editor: `32` capturas, `224` rectángulos y `1.984` glifos, `0` fallos; ambas ofertas/zonas, ocho formatos con cutouts simulados y tablet. Precio `int.MaxValue`, pulso máximo, contador visible y feedback sin saldo comprobado en Normal/Destacado/Presionado. Galería `TestResults/InGameShopPreview`.
- APK Development: `253,55 s`, `514.351.290 bytes`, `0 errores/2 advertencias`; SHA-256 `DFF515EBAD2572BFBA8FE721DF76C20E20C7422D839FECC576934CF6FBE7C636`. Instalación `-r` Success en Redmi Pad Pro y arranque frío correcto. Saldo visible de 32 conservado; hashes de perfil, records y catálogo iguales entre antes y después del primer arranque de la actualización.
- Windows `None`: `24,34 s`, `0 errores/2 advertencias`, composición aprobada en ambos prebuilds. Smoke oculto de 15 s vivo y responsivo, sin coincidencias críticas; no certifica una run interactiva ni el tamaño efectivo de ventana.

En la tablet se comprobaron además el cómic inicial, movimiento, pausa, tres derrotas naturales/reintentos y regreso a MainMenu. El log filtrado por PID registró `0` coincidencias críticas. No se alcanzó el contacto con DealerFish en esas runs: la compra física de la tienda queda pendiente dentro del checklist de overlays, con los casos de compra/expiración cubiertos en PlayMode. No se modificaron moneda, spawns ni estados internos para forzarla; el saldo final visible es 33 por recolección normal.

Ambos artefactos corresponden a `698d49d` más el cambio entrante de `TimeManager.asset`, preservado sin commit con SHA-256 `3396F52FDC16D445024E01D7A2D99DB6F0A37569D1D73C0302E9FFB857D90287`. Preservados bajo `Build/AndroidReview/698d49d` y `Build/WindowsReview/698d49d`; evidencia ignorada `TestResults/runshop-20261023-*`. No se afirma visita física a Abisopelágica, orientación opuesta, Back global, ciclo de vida ni rendimiento sostenido. Releases publicadas sin actualizar.

### Cómics narrativos con área segura — 24–25 de octubre de 2026

Fuente de componentes `201748f` y de integración `df74208`. El prefab compartido protege imagen y botón mediante safe area y contenido de referencia; mantiene la proporción del sprite y separa la imagen del botón visible. El cómic de inicio recibe Submit en «Jugar»; los temporizados retiran la selección del fondo y restauran el foco al cerrar si todavía les pertenece. Se conservan sprites, geometría desktop, persistencia de primera visualización y tiempos: inicio espera confirmación, portal/tienda/derrota terminan automáticamente a los 3 s. Las salidas de tienda sin sprite se omiten.

- EditMode: `334/334`; PlayMode completo: `17/17`, con repetición de los siete casos de tienda/cómics `7/7`, sin fallos ni omitidas. Inicio, portales, tienda con/sin compra y derrota devuelven control una sola vez; los límites y el aislamiento de las fixtures se detallan en `Testing.md`.
- Matriz Editor: `80` capturas en ocho formatos, `2` solicitudes omitidas por ausencia intencional de sprite y `0` fallos. Galería `TestResults/LoreComicPreview`; incluye cutouts a ambos lados y tablet 16:10. No certifica ergonomía ni navegación en hardware por sí sola.
- APK Development: `Succeeded`, `276,55 s`, `514.364.710 bytes`, `0 errores/2 advertencias`. SHA-256 `DDFFCF9B560B4A7711F0E2E560776C76C3EF5D97C2EBA46905814E12A2942087`.
- Windows `None`: `Succeeded`, `30,04 s`, `0 errores/2 advertencias`. Smoke oculto de 15 s vivo/responsivo y `0` coincidencias críticas. No certifica run interactiva ni tamaño efectivo de ventana. Ambos prebuilds aprobaron composición de escenas y prefabs.

El APK se actualizó mediante `adb install -r` en Redmi Pad Pro (Android 16). Perfil, records y catálogo conservaron sus SHA-256 entre antes de instalar y después del primer arranque. La revisión física comprobó el cómic inicial completo, su botón táctil, entrada a Epipelágica, pausa, reanudar con avance del puntaje, nueva pausa y regreso a MainMenu. El log del proceso registró `0` coincidencias críticas; la tablet queda en MainMenu con saldo 33. No se forzó moneda, spawn ni estado de juego.

Los otros consumidores del cómic están cubiertos por PlayMode y la matriz visual; no se visitaron físicamente en este pase. Sigue pendiente la compra por contacto natural con DealerFish dentro de la revisión integral de overlays, además de Abisopelágica física, orientación opuesta, Back, ciclo de vida y rendimiento sostenido.

Artefactos locales en `Build/AndroidDevelopment` y `Build/WindowsRelease`, fuente `df74208` más el mismo `TimeManager.asset` entrante preservado. Evidencia ignorada `TestResults/comics-20261024-*` (EditMode/preview), `TestResults/comics-20261025-*` (gates finales/builds/tablet/Windows) y galería. Estas builds son de revisión; no se actualizan las releases publicadas.

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

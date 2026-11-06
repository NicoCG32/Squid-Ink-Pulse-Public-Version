# Android sin conexión y modo feria

El juego Android funciona de forma local: arranca, muestra menús y cómics, permite jugar y conserva el perfil sin conexión. El complemento de feria queda deshabilitado en Android; el arranque no consulta `/health` ni crea una sesión o un menú de feria.

## Separación por plataforma

`FairModeSettings` aplica la política de plataforma antes de inicializar el complemento. Android no lo activa mediante una URL guardada ni mediante argumentos del ejecutable. El cliente HTTP de feria se excluye al compilar el player Android; una llamada directa recibe el resultado `fair_unavailable` sin realizar una petición.

En Windows se conserva el cliente de feria, la selección de servidor y el argumento `--fair-disabled`. El arranque puede comprobar el servidor configurado y abrir el acceso a feria cuando está disponible. Las instrucciones de operación están en [Servidor de feria](FairServer.md).

El perfil, récords y catálogo siguen almacenados en `Application.persistentDataPath/db/`. El modo avión no cambia la ubicación ni las reglas del guardado.

## Permisos del APK normal

El manifiesto final de la build Android sin Development declara:

| Permiso | Estado | Uso |
| --- | --- | --- |
| `android.permission.INTERNET` | Ausente | El juego local no requiere acceso de red. |
| `com.yecoworks.squidinkpulse.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` | Declarado y utilizado, nivel `signature` | Protección interna de receptores de AndroidX Core. |

La protección interna viene de AndroidX Core 1.9.0 en la composición Gradle comprobada. Su declaración utiliza el nivel `signature`, como especifica el [manifiesto de AndroidX Core](https://android.googlesource.com/platform/prebuilts/sdk/+/5c762fb2f6235bbe5d006d3a26063a8e921354b4/current/androidx/manifests/androidx.core_core/AndroidManifest.xml). No requiere una solicitud de permiso peligroso durante el juego.

Excluir el cliente HTTP no bastó para retirar `INTERNET` del manifiesto generado por Unity. `AndroidOfflineManifest` aplica por eso una directiva de merge en el manifiesto launcher generado para las builds Android sin Development. La regla conserva la actividad, los iconos y las demás declaraciones; no modifica los manifiestos de `Assets` ni los ajustes guardados del proyecto. Las opciones reales proceden del `BuildReport`.

El postproceso no actúa sobre Development ni sobre Windows. El APK de distribución debe comprobarse después del merge con `aapt dump permissions`: revisar sólo el XML previo a Gradle no demuestra qué permisos llegaron al paquete. Unity describe la generación automática y el merge de permisos en [Permisos Android en Unity](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/developing/device-features-and-permissions/permissions-in-unity).

## Comprobación del 5 de noviembre de 2026

- **361/361 EditMode:** política de plataforma, opciones de build, merge repetido, preservación de declaraciones y contrato de Development.
- **29/29 PlayMode:** recorridos funcionales sobre el mismo runtime; el ajuste posterior sólo añade tooling de manifiesto en el Editor.
- Builds Android y Windows sin Development, sin errores.
- Redmi Pad Pro: actualización conservando el perfil, arranque en frío con modo avión y Wi-Fi apagado, menú/cómic/Epipelágica/pausa/reanudación correctos. Conectividad restaurada al terminar; log sin errores críticos ni consulta de feria.
- Windows: arranque con feria habilitada y respuesta correcta de un servidor `/health` aislado; sin crear participantes, enviar compras o cambiar progreso. Esta comprobación cubre el acceso inicial, no una sesión completa del evento.

La build local comprobada procede de `8e408c4`; las releases anteriores conservan sus propios binarios. La compilación y los siguientes gates de distribución se explican en [AndroidBuild.md](AndroidBuild.md).

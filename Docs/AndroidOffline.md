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

## Verificar el paquete

Inspeccionar los permisos del APK final con `aapt dump permissions`. Comprobar arranque en frío, menú, cómic, partida y pausa con modo avión y Wi-Fi apagado. Al terminar, restaurar la conectividad del dispositivo de prueba.

En Windows, revisar por separado el arranque local con `--fair-disabled` y el acceso al servidor si se distribuye el complemento. [Build Android](AndroidBuild.md) detalla la compilación y [pruebas y validación](03-desarrollo/pruebas-y-validacion.md) reúne los recorridos de entrega.

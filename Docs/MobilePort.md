# Adaptación móvil

Android comparte la base de gameplay de Windows y añade entrada táctil, interfaz con área segura y coordinación del ciclo de vida. La versión móvil continúa en beta.

## Plataforma

| Ajuste | Configuración |
| --- | --- |
| Plataforma | Android ARM64, IL2CPP |
| Versión mínima | Android 7.1 / API 25 |
| Orientación | Landscape, con rotación izquierda/derecha; portrait deshabilitado |
| Formatos de interfaz | 16:9, 19,5:9, 20:9 y tablet 16:10 |
| Área segura | `Screen.safeArea` |
| Datos | JSON local en `Application.persistentDataPath/db/` |
| Red | Juego local; complemento de feria deshabilitado |
| Distribución actual | APK beta; candidata con firma de distribución pendiente |

## Entrada y presentación

`SquidInkPulseGameplayInputReader` publica comandos semánticos de movimiento, Ink-Pulse, pausa y gadgets. `TouchSteeringSurface` captura un dedo sobre el área de juego y los botones publican los mismos comandos que consume escritorio. Soltar el dedo conserva la altura y el avance horizontal continúa.

Los gestos iniciados sobre botones quedan separados del steering. Pausar, abrir una tienda o cómic, perder foco o cambiar de escena libera el contacto; continuar requiere una pulsación nueva. El contrato completo está en [Input](Input.md).

Las zonas comparten prefabs de HUD, controles y ajuste de referencia. Los menús, opciones, tiendas y cómics adaptan su composición al área segura. Resolución de ventana y fullscreen se ocultan en Android. El volumen se conserva al cerrar y reabrir.

## Interrupciones y guardado

[Atrás y el ciclo de vida](AndroidLifecycle.md) tienen una política común: cerrar el contexto apropiado y volver de segundo plano con la partida pausada. El audio y los comandos pendientes se coordinan con los avisos de foco y pausa de Android.

Las semillas se cargan como `TextAsset` desde `Resources/PersistentDbSeeds`; los guardados se escriben en el directorio persistente del dispositivo. El juego conserva perfiles y migraciones; cerrar el proceso inicia de nuevo el menú y conserva el progreso permanente. Consulta [persistencia local](PersistentProfile.md).

El player Android excluye el cliente HTTP de feria y la build normal se empaqueta sin permiso de Internet. Los detalles están en [Android sin conexión](AndroidOffline.md).

## Pruebas en dispositivo

El trabajo móvil ha utilizado **POCO X6 5G** y **Redmi Pad Pro**, ambos con Android 16/ARM64. El teléfono cubre un formato extra ancho y la tablet un formato 16:10. La evaluación final debe ampliar la cobertura a un teléfono de menores recursos y a sesiones prolongadas.

### Recorrido de partida

1. Iniciar desde MainMenu y controlar al calamar mediante touch.
2. Cargar y activar Ink-Pulse, realizar graze y usar un gadget disponible.
3. Abrir y cerrar una oferta de la tienda temporal.
4. Cruzar un portal hacia Abisopelágica conservando controles, HUD y estado temporal.
5. Llegar a Game Over, reintentar y regresar al menú.

### Recorridos de plataforma

| Área | Comprobación |
| --- | --- |
| Instalación | Instalar desde cero y actualizar sobre una versión compatible conservando progreso. |
| Menús | Opciones, Cómo jugar, tienda permanente, compra y equipamiento. |
| Narrativa | Cómics de inicio, portal, tienda y derrota. |
| Navegación | Atrás en cada contexto; evitar compras o reintentos accidentales. |
| Interrupciones | Home, cambio de aplicación y bloqueo de pantalla en ambas zonas y overlays. |
| Reapertura | Cierre del proceso desde segundo plano y arranque con datos persistidos. |
| Pantalla | Ambas orientaciones landscape, distintas proporciones y recortes laterales. |
| Sin conexión | Arranque y partida con modo avión y Wi-Fi apagado. |
| Rendimiento | Cargas, memoria y ritmo de presentación durante una sesión prolongada. |
| Regresión | Pruebas automáticas y recorrido Windows de los sistemas compartidos. |

## Herramientas

`AndroidReadinessAuditor` comprueba configuración, escenas, Input System, orientación, ARM64/IL2CPP y semillas sin modificarlos. Está en `Tools > Squid Ink Pulse > Audit Android Readiness`. En batch:

```powershell
& $unity -batchmode -nographics -quit -projectPath $PWD `
  -executeMethod AndroidReadinessAuditor.RunAndroidReadinessAudit `
  -logFile TestResults/android-readiness.log
```

Los errores producen una salida distinta de cero. [Build Android](AndroidBuild.md) explica la compilación, instalación y captura de logs; [Testing](Testing.md) reúne tests y exportadores de interfaz; [rendimiento Android](AndroidPerformance.md) describe las mediciones.

## Compatibilidad

Las adaptaciones de plataforma conservan dificultad, economía, daño, duración de Ink-Pulse y progresión. Las texturas y la calidad específicas de Android se comprueban sin alterar los recursos de Windows. iOS y la publicación en tiendas requieren una iniciativa y una cadena de firma propias.

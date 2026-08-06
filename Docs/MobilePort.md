# Port móvil

## Propósito

Este documento define el contrato técnico y de producto para portar Squid Ink-Pulse a teléfonos mediante Unity. El port extiende la base jugable existente; no reemplaza el juego de escritorio ni redefine su loop principal.

El primer objetivo de plataforma es Android. iOS queda como una extensión posterior porque requiere un entorno de compilación, firma y validación distinto.

## Estado

La iniciativa completó su fase de contrato y baseline. Todavía no existe una build móvil aceptada del producto. La referencia técnica Windows y su smoke interactivo aprobado están registrados en [MobileBaseline.md](MobileBaseline.md).

## Alcance del primer port

### Gameplay

La versión móvil debe conservar:

- movimiento continuo y límites de `PlayerBoundaries`;
- carga y activación de Ink-Pulse;
- graze, camarones, gadgets y economía de run;
- enemigos comunes, Ray, Jellyfish y bosses ya integrados;
- transición por portales entre `ZonaEpipelagica` y `ZonaAbisopelagica`;
- pausa, muerte, Game Over y retry.

La interacción móvil debe permitir completar una run sin teclado, mouse ni gamepad:

- mantener un dedo sobre la superficie de gameplay controla el objetivo vertical mediante la posición del contacto;
- la superficie de gameplay sólo conserva el control mientras ese contacto continúa activo;
- un botón explícito activa Ink-Pulse;
- un botón visible abre la pausa;
- los gadgets disponibles se ejecutan desde botones de sus slots;
- los gestos iniciados sobre botones no se interpretan también como movimiento;
- liberar, cancelar o perder el foco de un dedo no deja movimiento retenido.

### Definición de run completa

Para este port, una run completa es el recorrido verificable:

1. iniciar en `MainMenu` y comenzar una partida;
2. controlar al jugador en `ZonaEpipelagica` mediante touch;
3. cargar y activar Ink-Pulse, realizar graze y usar al menos un gadget disponible;
4. abrir y cerrar correctamente la tienda temporal cuando aparezca;
5. atravesar un portal hacia `ZonaAbisopelagica` y conservar controles, HUD y estado de run;
6. morir, visualizar el flujo de derrota y llegar a Game Over;
7. ejecutar retry o regresar a `MainMenu` sin perder el control ni dejar `Time.timeScale` bloqueado.

Ray, Jellyfish, bosses y eventos aleatorios deben conservar su comportamiento, pero una aparición concreta no se exige en cada repetición de este recorrido. Se validan también mediante recorridos dirigidos o checks separados.

### UI y navegación

Deben poder operarse mediante touch:

- `MainMenu`;
- opciones aplicables a móvil;
- comic de `Cómo Jugar` ya disponible;
- tienda permanente;
- HUD de ambas zonas;
- pausa y Game Over;
- tienda temporal de run;
- comics narrativos de inicio, portales, tienda y derrota.

La UI debe respetar el área segura del dispositivo, funcionar en las relaciones de aspecto objetivo y conservar estados pressed, disabled y selected sin depender de hover de mouse.

Las opciones exclusivas de escritorio —por ejemplo, cambiar resolución de ventana o fullscreen— no deben presentarse como si fueran aplicables a un teléfono. Volumen y preferencias que sí tengan sentido en móvil deben conservarse.

### Datos locales

El port debe conservar por dispositivo:

- perfil del jugador;
- camarones y economía;
- mejoras permanentes y skins equipadas;
- gadgets desbloqueados;
- records y leaderboard local;
- comics cuya visualización se persiste.

Los archivos runtime continúan bajo `Application.persistentDataPath/db/`. Las semillas empaquetadas no deben asumirse como archivos ordinarios cuando la aplicación se ejecuta dentro de un APK; la implementación deberá utilizar un proveedor compatible con Android y mantener una fuente única de datos.

La ausencia de red no debe impedir el arranque ni el gameplay local.

### Ciclo de vida Android

El juego debe definir un comportamiento estable para:

- botón Back del sistema;
- pérdida y recuperación de foco;
- suspensión y reanudación;
- bloqueo de pantalla;
- terminación del proceso desde background.

Back se interpreta como la acción contextual `Cancel` y sigue este contrato:

| Contexto | Resultado |
| --- | --- |
| `MainMenu` raíz | No realiza una salida inmediata. Se ignora mientras no exista una confirmación explícita de salida. |
| Opciones, tutorial gráfico o subpanel de menú | Cierra el subpanel actual y vuelve a su pantalla anterior. |
| Tienda permanente | Regresa a `MainMenu`. |
| Gameplay activo | Abre la pausa. |
| Pausa | Reanuda la run si no hay otro subpanel abierto. |
| Opciones abiertas desde pausa | Cierra opciones y regresa a pausa. |
| Tienda temporal o comic narrativo | Ejecuta únicamente la acción de cierre o continuación que ese overlay permita; nunca confirma una compra. |
| Game Over | No ejecuta retry ni abandona la pantalla sin una acción explícita del usuario. |

Back no debe terminar una run, confirmar una compra ni saltarse una decisión irreversible por accidente.

El estado guardable pendiente debe persistirse antes de que la aplicación abandone el primer plano. Reanudar debe ser idempotente: no puede duplicar listeners, overlays, audio ni acción de input.

### Add-on de feria

El add-on de feria no condiciona el port inicial. En Android debe quedar deshabilitado o inerte, sin probes de red que impidan el arranque ni permisos innecesarios. El modo feria de Windows conserva su contrato documentado en [FairServer.md](FairServer.md).

## Plataforma y formato inicial

| Decisión | Contrato inicial |
| --- | --- |
| Plataforma | Android |
| Orientación | Landscape fijo con autorrotación entre landscape left y landscape right |
| Portrait | Deshabilitado |
| Movimiento touch | Seguimiento vertical continuo de un contacto activo sobre la superficie de gameplay |
| Ink-Pulse | Botón explícito separado de la superficie de movimiento |
| Gadgets | Un botón explícito por slot utilizable; slots no utilizables ocultos o deshabilitados |
| Pausa y Back | Botón visible de pausa y acción `Cancel` contextual según la tabla anterior |
| Formatos de teléfono | 16:9, 19.5:9 y 20:9 |
| Cutout/notch | Debe respetarse mediante `Screen.safeArea` |
| Tablets y plegables | Compatibilidad deseable, no criterio bloqueante inicial |
| Artefacto de desarrollo | APK instalable |
| Artefacto candidato | AAB reproducible |
| Servicios remotos | No requeridos |

La orientación, identificador, versión mínima, arquitectura, backend de scripting y nivel de calidad deben fijarse en configuración versionada y comprobarse mediante una validación automática de Editor. No deben quedar como decisiones manuales del equipo que genera la build.

## Auditor de preparación Android

`AndroidReadinessAuditor` inspecciona la configuración sin modificarla. Comprueba:

- versión exacta de Unity y disponibilidad de Android Build Support;
- escenas habilitadas y su orden;
- Input System activo;
- Company Name e identificador Android;
- orientación landscape y rotaciones permitidas;
- ARM64 e IL2CPP;
- presencia de las cuatro semillas de persistencia.

Desde Unity se ejecuta mediante:

```text
Tools > Squid Ink Pulse > Audit Android Readiness
```

Desde la raíz del repositorio en PowerShell, con el proyecto cerrado en otras instancias de Unity:

```powershell
New-Item -ItemType Directory -Force "$PWD\TestResults" | Out-Null

& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -quit `
  -projectPath "$PWD" `
  -executeMethod AndroidReadinessAuditor.RunAndroidReadinessAudit `
  -logFile "$PWD\TestResults\android-readiness.log"
```

La salida distingue `INFO`, `WARNING` y `ERROR`. En menú, los errores se informan sin corregir valores. En batch, cualquier `ERROR` produce código de salida distinto de cero para bloquear una build o integración incompatible. `TestResults/` permanece ignorado por Git.

## Rendimiento y tamaño

No se prescribe una optimización de arte por anticipado. Primero debe medirse en hardware real:

- frame time y FPS durante una run representativa;
- memoria y picos al cambiar de zona o abrir UI pesada;
- tiempo de arranque y carga de escenas;
- tamaño de APK/AAB y tamaño instalado;
- comportamiento en una sesión sostenida y evidencia de throttling;
- errores y hotspots observados en el Profiler.

Los cambios de URP, calidad, textura, audio, shader o runtime se aceptan sólo si tienen una medición antes/después y una verificación visual o auditiva equivalente. El perfil móvil no debe degradar intencionalmente el perfil Windows.

## Matriz de validación

La siguiente matriz es el mínimo contractual. Los modelos concretos, versión de Android, resolución y memoria deben registrarse cuando se disponga de los dispositivos.

| Entorno | Cobertura | Resultado requerido |
| --- | --- | --- |
| Unity Editor 16:9 | Regresión rápida de layout y gameplay | Sin errores y sin referencias rotas |
| Unity Editor 19.5:9 / 20:9 | Aspect ratio y safe area simulada | Sin recortes ni solapamientos críticos |
| Emulador Android | Instalación, arranque, escenas y logs | `MainMenu` abre sin red |
| Teléfono Android de gama baja | Touch, memoria, FPS y suspensión | Cumple presupuesto aceptado |
| Teléfono Android de gama media | Experiencia objetivo y compatibilidad | Run completa y UI operable |
| Windows | Regresión teclado/mouse y persistencia | Contrato desktop preservado |

### Cupos de dispositivos

Los cupos se mantienen explícitos aunque todavía no haya un modelo asignado. La ausencia de emulador, teléfono de referencia baja o PC de regresión impide aceptar la candidata final, pero no bloquea la implementación previa que pueda validarse en Editor. El teléfono de referencia media es obligatorio si está disponible; de lo contrario, su ausencia debe quedar registrada como limitación.

| Cupo | Modelo | Sistema | RAM | Resolución | Estado | Condición de candidata |
| --- | --- | --- | --- | --- | --- | --- |
| Emulador Android | Por asignar | Por asignar | Por asignar | 16:9, 19.5:9 y 20:9 simulados | Pendiente | Obligatorio para bootstrap |
| Teléfono Android de referencia baja | Por asignar | Por asignar | Por asignar | Por registrar | Pendiente | Obligatorio |
| Teléfono Android de referencia media | Por asignar | Por asignar | Por asignar | Por registrar | Pendiente | Obligatorio si está disponible |
| PC de regresión Windows | Ryzen 5 7500F / RTX 5060 Ti | Windows 11 Pro `10.0.26200` | 31,6 GiB | Medición a 1280x720; monitor 1920x1080 | Baseline técnico y smoke interactivo aprobados | Obligatorio |

Los modelos, versiones, memoria y resoluciones deben completarse antes de fijar presupuestos finales de rendimiento. Las métricas obtenidas en dispositivos distintos no se comparan como si pertenecieran al mismo baseline.

### Flujos obligatorios fuera de una run

Además de la run completa, la candidata debe verificar por separado:

1. opciones aplicables a móvil y persistencia de preferencias;
2. comic `Cómo Jugar` y regreso a `MainMenu`;
3. navegación, compra y equipamiento en la tienda permanente;
4. comics narrativos de inicio, portal, tienda y derrota;
5. primer arranque, cierre forzado, reapertura y actualización sobre datos existentes;
6. comportamiento de Back, pérdida de foco, suspensión, bloqueo de pantalla y retorno.

En cada dispositivo móvil deben ejecutarse, como mínimo:

1. instalación limpia y primer arranque;
2. arranque en modo avión;
3. Main Menu, opciones, comic tutorial y tienda permanente;
4. run con movimiento, graze, Ink-Pulse, gadgets, Ray y Jellyfish;
5. transición por portal a la segunda zona;
6. pausa, reanudación, Game Over, retry y regreso al menú;
7. tienda temporal y comics narrativos cuando correspondan;
8. botón Back en cada contexto;
9. suspensión, bloqueo y retorno a la aplicación;
10. cierre forzado y reapertura con datos persistidos;
11. actualización instalada sobre una versión previa;
12. sesión sostenida para detectar degradación progresiva.

## Criterios de aceptación

El primer port móvil se considera aceptable cuando:

- un APK Android instala y abre `MainMenu` sin servicios externos;
- una run básica se completa sólo con touch;
- Ink-Pulse, pausa, gadgets, Game Over, tiendas, opciones y comics son operables;
- la pantalla no corta controles ni información crítica en los formatos objetivo;
- la persistencia sobrevive suspensión, cierre y reapertura;
- el botón Back tiene comportamiento contextual documentado;
- el add-on de feria no bloquea la experiencia local;
- existen métricas mínimas de rendimiento y memoria en hardware real;
- existe un AAB candidato reproducible sin versionar secretos;
- las pruebas automatizadas pertinentes pasan y Windows no presenta regresiones no autorizadas.

## Fuera de alcance

No forma parte de este primer port:

- iOS y su cadena de firma;
- publicación en Google Play o App Store;
- cloud save, login, analítica, publicidad o crash reporting;
- sincronización remota de perfiles o feria móvil;
- rediseño completo del juego;
- nuevas zonas, enemigos, bosses o skins;
- tablets como garantía contractual;
- reactivación del tutorial jugable aislado;
- optimización masiva de assets sin evidencia.

## Regla de compatibilidad

Toda modificación móvil debe indicar si:

1. conserva el comportamiento del producto base;
2. adapta su presentación o entrada sólo a la plataforma;
3. amplía el contrato de producto.

Una adaptación de plataforma no debe introducir silenciosamente cambios de dificultad, economía, spawn, daño, duración de Ink-Pulse ni progresión. Si el port revela un defecto común a todas las plataformas, se debe tratar como una corrección separada y verificable.

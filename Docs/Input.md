# Contrato de entrada

## Propósito

La entrada de Squid Ink-Pulse se expresa mediante comandos del juego y no mediante dispositivos concretos. La fuente versionada es `Assets/Implementation/Config/Input/InputSystem_Actions.inputactions`; los nombres compartidos por código viven en `Assets/Implementation/Code/Input/SquidInkPulseInputContract.cs`.

El wrapper C# automático del Input System permanece deshabilitado. Los consumidores deben usar las constantes del contrato en vez de repetir nombres como strings.

## Estado actual

`SquidInkPulseGameplayInputReader` ya es la frontera única entre el Input System y el dominio. `SquidInkPulseInputRuntime` apaga el asset project-wide antes de la primera escena; `SquidInkPulseGameplayInputScope`, incorporado al prefab del jugador, crea y habilita el lector sólo mientras existe gameplay. El lector expone la posición continua, solicitudes discretas y el esquema de control vigente sin referenciar movimiento, habilidades, pausa ni inventario.

Los controladores de gameplay todavía leen mouse o teclado directamente; su migración se realizará en cortes posteriores. Por tanto, incorporar el lector conserva el comportamiento Windows actual y todavía no hace operativo el gameplay mediante touch.

Los `InputSystemUIInputModule` canónicos continúan usando las acciones UI predeterminadas del paquete de Unity. El mapa `UI` versionado conserva los mismos nombres, tipos y controles requeridos para permitir una migración atómica posterior. Unity habilita automáticamente todo asset configurado como project-wide al entrar en Play Mode; el bootstrap corrige ese estado mediante `InputActionAsset.Disable()` y cada scope habilita exclusivamente `Gameplay`, evitando una segunda copia activa de `UI`.

## API del lector

El estado continuo se consulta mediante `SteerPosition`. Los comandos discretos se entregan inmediatamente mediante:

- `InkPulseRequested`;
- `PauseToggleRequested`;
- `GadgetSlot1Requested`;
- `GadgetSlot2Requested`.

Cada solicitud se emite sólo durante la fase `performed`; mantener o liberar un botón no repite el comando. El lector no conserva flags ni colas de comandos discretos: deshabilitarlo desuscribe el mapa y limpia posición y esquema. Al habilitar un ciclo nuevo fija una barrera temporal y rechaza eventos encolados antes o durante esa transición. Destruir el prefab del jugador dispone el lector y libera sus suscriptores, de modo que una solicitud de una escena no se reproduzca en la siguiente.

`CurrentControlScheme` y `ControlSchemeChanged` centralizan el dispositivo activo. La resolución intenta primero emparejar todos los dispositivos disponibles con los esquemas del asset y exige incluir el control que produjo la acción; si el conjunto está incompleto, usa el primer esquema que soporte ese dispositivo. Así `Keyboard&Mouse`, `Gamepad` y el futuro input Touch no requieren condicionales repartidos por controladores.

## Mapa `Gameplay`

| Acción | Tipo | Binding Keyboard&Mouse | Semántica |
| --- | --- | --- | --- |
| `SteerPosition` | `Value / Vector2` | `<Mouse>/position` | Posición de pantalla usada para resolver el objetivo vertical. |
| `ActivateInkPulse` | `Button` | Click izquierdo y `Space` | Solicita una única activación de Ink-Pulse. Las reglas de carga y bloqueo permanecen en gameplay. |
| `TogglePause` | `Button` | `P` y `Escape` | Solicita alternar la pausa en el contexto jugable. |
| `UseGadgetSlot1` | `Button` | `Q` | Solicita usar el gadget disponible en el slot 1. |
| `UseGadgetSlot2` | `Button` | `W` | Solicita usar el gadget disponible en el slot 2. |

El GUID del asset y el ID del mapa se conservan. Las cinco entradas reutilizan IDs estables del mapa previo después de comprobar que no existían `InputActionReference`, consumidores ni rebindings persistidos; su semántica vigente queda definida por los nombres y constantes actuales, no por los nombres genéricos anteriores. Todos los nombres de plantilla que no representan el dominio (`Player`, `Attack`, `Crouch`, `Sprint`, etc.) fueron retirados.

## Touch sin doble consumo

No existe ningún binding directo `<Touchscreen>` bajo `Gameplay`. En particular, no se permite enlazar `primaryTouch/tap` a Ink-Pulse: un tap general también puede ser un click de UI o comenzar sobre otro control.

Touch pertenece por ahora al mapa `UI`:

- `Point` recibe `<Touchscreen>/touch*/position`;
- `Click` recibe `<Touchscreen>/touch*/press`;
- `Submit` y `Cancel` conservan el contrato estándar de interfaz.

La superficie de movimiento y los botones de Ink-Pulse, pausa y gadgets deberán inyectar comandos desde regiones explícitas. La superficie rechazará gestos iniciados sobre botones; los botones no alimentarán movimiento. Esa integración no se simula mediante un tap global en el asset.

## Compatibilidad de interfaz

El mapa `UI` conserva estas acciones requeridas por `InputSystemUIInputModule`:

- `Navigate`, `Submit` y `Cancel`;
- `Point`, `Click`, `RightClick`, `MiddleClick` y `ScrollWheel`;
- `TrackedDevicePosition` y `TrackedDeviceOrientation`.

Sus bindings existentes de Keyboard&Mouse, Gamepad, Touch, Joystick y XR se mantienen. Normalizar grupos vacíos no cambia los dispositivos asociados.

## Validación

`InputActionContractTests` carga el asset mediante su importer real de Unity y comprueba:

- mapa y acciones semánticas exactas;
- bindings de escritorio equivalentes al producto base;
- ausencia de touchscreen en gameplay;
- propiedad exclusiva de `UI/Point` y `UI/Click` sobre los bindings touch;
- tipos requeridos por `InputSystemUIInputModule`;
- unicidad de IDs de mapas, acciones y bindings.

`GameplayInputReaderTests` clona el asset y aísla el Input System mediante dispositivos simulados. Comprueba:

- normalización desde el autoarranque project-wide a sólo `Gameplay`;
- habilitación y deshabilitación idempotentes;
- actualización y limpieza de la posición continua;
- una sola solicitud por pulsación para los cuatro comandos discretos;
- cambio centralizado de `Keyboard&Mouse` a `Gamepad`;
- descarte de un comando encolado aunque el lector vuelva a habilitarse antes del siguiente update.

La sensación de control, las regiones touch explícitas y el multitouch final se validan además en hardware real.

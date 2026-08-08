# Contrato de entrada

## Propósito

La entrada de Squid Ink-Pulse se expresa mediante comandos del juego y no mediante dispositivos concretos. La fuente versionada es `Assets/Implementation/Config/Input/InputSystem_Actions.inputactions`; los nombres compartidos por código viven en `Assets/Implementation/Code/Input/SquidInkPulseInputContract.cs`.

El wrapper C# automático del Input System permanece deshabilitado. Los consumidores deben usar las constantes del contrato en vez de repetir nombres como strings.

## Estado actual

El asset ya define el mapa semántico y sus bindings de escritorio, pero los controladores de gameplay todavía leen mouse o teclado directamente. La migración de `PlayerMovement`, `InkPulseController`, pausa e inventario se realizará mediante un lector único en cortes posteriores. Por tanto, este contrato no implica todavía gameplay operativo mediante touch.

Los `InputSystemUIInputModule` canónicos continúan usando las acciones UI predeterminadas del paquete de Unity. El mapa `UI` versionado conserva los mismos nombres, tipos y controles requeridos para permitir una migración atómica posterior. Mientras ambos existan, el futuro lector de gameplay debe habilitar sólo `Gameplay`; no debe activar una segunda copia de `UI`.

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

Las pruebas con dispositivos simulados, consumo único, habilitación/deshabilitación de mapas y cambio de esquema pertenecen al lector runtime. La sensación de control y el multitouch final se validan además en hardware real.

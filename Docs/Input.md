# Contrato de entrada

## Propósito

La entrada de Squid Ink-Pulse se expresa mediante comandos del juego y no mediante dispositivos concretos. La fuente versionada es `Assets/Implementation/Config/Input/InputSystem_Actions.inputactions`; los nombres compartidos por código viven en `Assets/Implementation/Code/Input/SquidInkPulseInputContract.cs`.

El wrapper C# automático del Input System permanece deshabilitado. Los consumidores deben usar las constantes del contrato en vez de repetir nombres como strings.

## Estado actual

`SquidInkPulseGameplayInputReader` ya es la frontera única entre el Input System y el dominio. `SquidInkPulseInputRuntime` apaga el asset project-wide antes de la primera escena; `SquidInkPulseGameplayInputScope`, incorporado al prefab del jugador, crea y habilita el lector sólo mientras existe gameplay. El lector expone la posición continua, solicitudes discretas y el esquema de control vigente sin referenciar controladores concretos.

`PlayerMovement`, `InkPulseController`, `PauseMenuManager`, `PlayerGadgetInventory` e `InGameShopManager` ya consumen el lector semántico. Los bindings continúan siendo mouse/teclado, por lo que este cierre conserva el comportamiento Windows pero todavía no hace operativo el gameplay mediante touch.

La tecla `B` de la tienda temporal queda clasificada como atajo de producto exclusivo de escritorio y se expresa mediante `Gameplay/BuyShopOffer`, con un único binding Keyboard&Mouse y ninguno Touch. `InGameShopManager` consume la solicitud y la dirige a la misma autoridad `BuyCurrentOffer()` usada por `buyButton.onClick`; el botón `Comprar` es la vía móvil. Las únicas lecturas directas de teclado que permanecen están en el código secreto de menú y pertenecen a entrada de texto, no a gameplay.

Los `InputSystemUIInputModule` canónicos continúan usando las acciones UI predeterminadas del paquete de Unity. El mapa `UI` versionado conserva los mismos nombres, tipos y controles requeridos para permitir una migración atómica posterior. Unity habilita automáticamente todo asset configurado como project-wide al entrar en Play Mode; el bootstrap corrige ese estado mediante `InputActionAsset.Disable()` y cada scope habilita exclusivamente `Gameplay`, evitando una segunda copia activa de `UI`.

## API del lector

`HasSteerPosition` indica si el ciclo actual dispone de un control continuo válido; `SteerPosition` entrega esa posición en píxeles de pantalla. Al habilitarse, el lector siembra de inmediato el valor del control resuelto, incluso si el mouse está en `(0,0)`. Esa coordenada es válida y no representa ausencia de input. Al perderse el dispositivo se invalida el objetivo; una reconexión vuelve a sembrar su estado sin reutilizar la última posición del dispositivo anterior.

Los comandos discretos se entregan inmediatamente mediante:

- `InkPulseRequested`;
- `PauseToggleRequested`;
- `GadgetSlot1Requested`;
- `GadgetSlot2Requested`;
- `ShopPurchaseRequested`.

Cada solicitud se emite sólo durante la fase `performed`; mantener o liberar un botón no repite el comando. El lector no conserva flags ni colas de comandos discretos: deshabilitarlo desuscribe el mapa y limpia posición y esquema. Al habilitar un ciclo nuevo fija una barrera temporal y rechaza eventos encolados antes o durante esa transición. Destruir el prefab del jugador dispone el lector y libera sus suscriptores, de modo que una solicitud de una escena no se reproduzca en la siguiente.

`InkPulseInputBinding` recibe `InkPulseRequested`, conserva como máximo una solicitud pendiente y la entrega al siguiente `InkPulseController.Update`, donde se llama a `TryActivatePulse()`. Esto preserva el orden del polling original y combina click+`Space` simultáneos en un único intento. La clase conserva la referencia exacta al lector al que se suscribió y la libera de forma idempotente. `SquidInkPulseInputRuntime.GameplayChanged` permite reemplazar el binding si el scope se recrea mientras el controlador sigue habilitado. Las reglas de sesión, carga, pulso activo, tienda y transición de portal permanecen en `InkPulseActivationPolicy` y en el controlador; el lector sólo solicita la acción.

`GameplayCommandInputBinding` aplica el mismo buffer de una solicitud a pausa y compra, y mantiene buffers independientes para los dos slots. Los controladores consumen y limpian esos flags al comienzo de cada `Update`, antes de sus guards, por lo que una pulsación durante animación, pausa, tienda cerrada o bloqueo de tienda no queda latente. `PlayerGadgetInventory.TryUseSlot1()` y `TryUseSlot2()` son las operaciones públicas para botones futuros y conservan dentro de sí todas las reglas de sesión, tienda, tipo activo, posesión, efecto y consumo. `InGameShopManager` conserva `BuyCurrentOffer()` como autoridad común para el comando Keyboard&Mouse y el botón UI.

`CurrentControlScheme` y `ControlSchemeChanged` centralizan el dispositivo activo. La resolución intenta primero emparejar todos los dispositivos disponibles con los esquemas del asset y exige incluir el control que produjo la acción; si el conjunto está incompleto, usa el primer esquema que soporte ese dispositivo. Así `Keyboard&Mouse`, `Gamepad` y el futuro input Touch no requieren condicionales repartidos por controladores.

## Mapa `Gameplay`

| Acción | Tipo | Binding Keyboard&Mouse | Semántica |
| --- | --- | --- | --- |
| `SteerPosition` | `Value / Vector2` | `<Mouse>/position` | Posición de pantalla usada para resolver el objetivo vertical. |
| `ActivateInkPulse` | `Button` | Click izquierdo y `Space` | Solicita una única activación de Ink-Pulse. Las reglas de carga y bloqueo permanecen en gameplay. |
| `TogglePause` | `Button` | `P` y `Escape` | Solicita alternar la pausa en el contexto jugable. |
| `UseGadgetSlot1` | `Button` | `Q` | Solicita usar el gadget disponible en el slot 1. |
| `UseGadgetSlot2` | `Button` | `W` | Solicita usar el gadget disponible en el slot 2. |
| `BuyShopOffer` | `Button` | `B` | Solicita comprar la oferta visible. Permanece Keyboard&Mouse-only; móvil usa el botón UI. |

El GUID del asset y el ID del mapa se conservan. Las primeras cinco entradas reutilizan IDs estables del mapa previo después de comprobar que no existían `InputActionReference`, consumidores ni rebindings persistidos; `BuyShopOffer` incorpora IDs nuevos para acción y binding. Su semántica vigente queda definida por los nombres y constantes actuales, no por los nombres genéricos anteriores. Todos los nombres de plantilla que no representan el dominio (`Player`, `Attack`, `Crouch`, `Sprint`, etc.) fueron retirados.

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
- estado inicial, reactivación, desconexión y reconexión del mouse, incluida la coordenada `(0,0)`;
- una sola solicitud por pulsación para los cinco comandos discretos;
- coalescencia por frame de click izquierdo y `Space`, desuscripción idempotente y recreación notificada del scope;
- buffers independientes de pausa, slots y compra, incluidos P+`Escape` y Q+W simultáneos, limpieza y Dispose idempotente;
- cambio centralizado de `Keyboard&Mouse` a `Gamepad`;
- descarte de un comando encolado aunque el lector vuelva a habilitarse antes del siguiente update.

`PlayerVerticalMovementPolicyTests` caracteriza la migración del primer consumidor: prioridad y consumo temporal del impulso de Jellyfish, reanudación del objetivo del jugador, movimiento sin overshoot, ausencia de movimiento sin objetivo y acumulación `max/max` de impulsos repetidos.

La sensación de control, las regiones touch explícitas y el multitouch final se validan además en hardware real.

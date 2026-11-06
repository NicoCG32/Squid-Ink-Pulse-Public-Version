# Navegación e interrupciones en Android

El juego utiliza Atrás como una orden contextual de Cancelar. La navegación móvil y la coordinación de interrupciones se instalan al arrancar el player móvil; no requieren modificar las escenas ni cambian los atajos de Windows.

## Atrás según la pantalla

| Contexto visible | Resultado |
| --- | --- |
| Opciones | Cierra opciones y vuelve al contexto anterior. Desde pausa, conserva la pausa. |
| Tutorial gráfico del menú | Cierra el tutorial. |
| Tienda permanente | Vuelve al menú principal. |
| Oferta de tienda durante la partida | Cierra la oferta sin comprar. Conserva el flujo narrativo de salida. |
| Partida activa | Abre pausa. |
| Pausa | Reanuda mediante la misma transición que el botón Reanudar. |
| Cómic o carga/transición en curso | Ignora Atrás; el cómic conserva su botón o temporizador. |
| Animación de pausa | Ignora pulsaciones adicionales hasta terminar la transición. |
| Game Over | Conserva la pantalla; Reintentar y Menú son acciones explícitas. |
| Menú principal | Ignora Atrás de forma deliberada. |

`MobileBackNavigation` posee una copia de la acción `UI/Cancel`, independiente del mapa de gameplay. Su política resuelve una sola acción por pulsación. En móvil, Escape deja de generar además el atajo antiguo de pausa; esto evita cerrar un panel y reanudar con la misma pulsación. El comportamiento de teclado de escritorio se conserva.

## Cambio de aplicación y bloqueo de pantalla

Al perder foco o recibir una pausa de la aplicación, el coordinador móvil deshabilita el canal de gameplay, descarta órdenes pendientes y libera el steering táctil. Pausa el audio sin cambiar el volumen guardado. Una partida activa pasa a pausa y requiere Reanudar al volver.

Android puede enviar los avisos de foco y pausa en distinto orden. La aplicación permanece suspendida mientras alguno indique segundo plano. Sólo restaura el canal de input y el estado anterior del audio cuando ambos permiten volver; nunca reanuda la partida automáticamente. Si la interrupción coincide con la animación de Reanudar, cancela esa animación y deja la pausa utilizable.

Si una tienda o cómic ya retiene el tiempo de juego, conserva esa pantalla y congela su temporizador durante la interrupción. Al volver, el usuario puede terminar ese contexto; antes de liberar el tiempo de gameplay aparece la pausa. Una carga que ya estaba iniciada conserva la necesidad de reanudar en la nueva escena.

Los checkpoints de preferencias existentes siguen guardando cambios pendientes al pasar a segundo plano. Terminar el proceso y abrirlo otra vez inicia el flujo normal del menú y conserva el progreso persistente; no implica restaurar una partida en curso.

## Verificación

- `MobileBackNavigationPolicyTests`: tabla de prioridades y acciones, incluidos cómic/transición, Game Over y menú principal.
- `MobilePlatformPlayModeTests`: Cancel real por teclado a través del Input System; menús y ambas zonas; órdenes pendientes, steering, orden/repetición de callbacks, pausa previa, animación de reanudación, cambio de escena, tienda y cómic interrumpidos, restauración del audio.
- Los tests usan un perfil transitorio del Editor. No modifican el perfil real para forzar ofertas, derrotas o cambios de escena.
- Las pruebas automáticas se complementan con Back, Home, bloqueo/desbloqueo y reapertura en hardware; no sustituyen esa comprobación.

Comprobación del 5 de noviembre de 2026: **344/344 EditMode y 29/29 PlayMode**, builds Android y Windows sin Development, sin errores. En Redmi Pad Pro se verificaron Atrás en menú, opciones, tutorial, tienda permanente, cómic y pausa; opciones desde pausa; regreso desde Home y bloqueo/desbloqueo durante Epipelágica. La partida volvió pausada y permitió reanudar; los tres JSON de progreso conservaron sus hashes durante la actualización y el recorrido. El smoke de arranque Windows de 15 s fue correcto. La interrupción física de DealerFish, Abisopelágica y la terminación del proceso desde segundo plano quedan para el recorrido ampliado; esos escenarios no se dan por comprobados en hardware por los tests del Editor.

Referencias: [OnApplicationPause](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/monobehaviour/onapplicationpause), [OnApplicationFocus](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/monobehaviour/onapplicationfocus), [comportamiento en segundo plano del Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/background-behavior.html).

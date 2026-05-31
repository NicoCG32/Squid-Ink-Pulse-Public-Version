# Resumen del proyecto

## Qué es

Squid Ink-Pulse es un endless runner 2D en Unity donde el jugador controla a un calamar bebé que avanza de forma continua en un entorno submarino hostil. La idea central no es evitar el peligro por completo, sino convertirlo en una fuente de ventaja mediante la mecánica Ink-Pulse.

## Pilares

- Riesgo y recompensa: acercarse a amenazas carga el recurso principal.
- Ritmo constante: el juego no se detiene salvo por los estados globales.
- Claridad visual: el entorno debe leerse rápido, incluso cuando la densidad sube.
- Rejugabilidad: la progresión de run y los eventos buscan que cada intento cambie lo suficiente.

## Estructura general

- `Assets/Implementation/Code/`: lógica de juego, UI, boss, cámara, spawn y utilidades.
- `Assets/Content/`: arte, audio, prefabs y animaciones runtime.
- `Assets/Scenes/`: escenas del juego.
- `Docs/`: documentación técnica, de diseño y de organización.

## Orden recomendado de lectura

1. [ProjectOverview.md](ProjectOverview.md)
2. [ProjectStructure.md](ProjectStructure.md)
3. [StateMachines.md](StateMachines.md)
4. [GameplaySystems.md](GameplaySystems.md)
5. [CoreSystems.md](CoreSystems.md)
6. [EnemiesAndBosses.md](EnemiesAndBosses.md)
7. [UiAndMenu.md](UiAndMenu.md)
8. [WorldAndCamera.md](WorldAndCamera.md)
9. [Roadmap.md](Roadmap.md)

# 02 · Documentación técnica

[← Índice](../README.md)

El código fuente es la fuente de verdad para las decisiones de implementación. Esta área resume sus fronteras y remite a las referencias especializadas cuando hace falta entrar en detalle.

| Documento | Pregunta que responde |
| --- | --- |
| [Arquitectura por niveles](arquitectura.md) | ¿Cómo se relacionan jugador, Unity, componentes y servidor opcional? |
| [Dominio y persistencia](dominio-y-persistencia.md) | ¿Qué datos existen y dónde se guardan? |
| [Estados y flujos](estados-y-flujos.md) | ¿Qué cambia al pausar, usar Ink-Pulse, cruzar portales o morir? |
| [Integración de feria](integracion-feria.md) | ¿Qué hace realmente el host Python/SQLite y qué no promete? |
| [Compilación y distribución](compilacion-y-distribucion.md) | ¿Cómo abrir, construir o instalar las versiones publicadas? |

### Componentes donde empezar a leer código

- `Assets/Implementation/Code/Core/`: sesión, escenas y progresión.
- `Assets/Implementation/Code/Player/`: movimiento, estado, habilidades y perfil.
- `Assets/Implementation/Code/Spawning/`: perfiles y aparición de amenazas.
- `Assets/Implementation/Code/Bosses/` y `Enemies/`: jefes y comportamiento.
- `Assets/Implementation/Code/UI/` y `Input/`: presentación e interacción.
- `Tools/FairServer/`: extensión de feria, separada del juego normal.

**No confundir:** los guardados JSON del juego son documentos locales; SQLite solo aparece en el servidor adicional de feria. No hay motivo para dibujar una base de datos SQL central del juego, porque esa base no existe.

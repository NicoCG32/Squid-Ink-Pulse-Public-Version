# Documentación de Squid Ink-Pulse

[← Portada](../README.md) · [Créditos y derechos](../LICENSING.md)

Las guías se organizan en cinco áreas. Los capítulos técnicos describen el código actual; las notas de cada [release](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases) acompañan a su descarga.

| Área | Contenido | Entrada |
| --- | --- | --- |
| **01 · General** | Origen, propuesta, público y equipo | [Presentación](01-general/README.md) |
| **02 · Técnica** | Arquitectura, datos, estados, compilación e integración de feria | [Mapa técnico](02-tecnica/README.md) |
| **03 · Desarrollo** | Método, estructura, pruebas, proyección y estimaciones | [Guía de desarrollo](03-desarrollo/README.md) |
| **04 · Académica y formativa** | Curso, entregas e informes originales | [Archivo académico](04-academica/README.md) |
| **05 · Diseño y experiencia de juego** | Mecánicas, controles, zonas, narrativa y presentación | [Diseño del juego](05-diseno-y-experiencia-de-juego/README.md) |

## Rutas de lectura

- **Jugar:** [manual](05-diseno-y-experiencia-de-juego/manual-del-jugador.md) → [descargas](02-tecnica/compilacion-y-distribucion.md) → [zonas y amenazas](05-diseno-y-experiencia-de-juego/zonas-y-amenazas.md).
- **Comprender el código:** [arquitectura](02-tecnica/arquitectura.md) → [dominio y persistencia](02-tecnica/dominio-y-persistencia.md) → [estados y flujos](02-tecnica/estados-y-flujos.md).
- **Contribuir:** [estructura](03-desarrollo/estructura-y-contribucion.md) → [pruebas](03-desarrollo/pruebas-y-validacion.md) → [roadmap](ROADMAP.md).
- **Revisar el trabajo universitario:** [contexto e informes](04-academica/README.md) → [diseño del juego](05-diseno-y-experiencia-de-juego/gdd.md).

## Referencias especializadas

| Tema | Guías |
| --- | --- |
| Arquitectura y escenas | [Arquitectura de software](SoftwareArchitecture.md) · [Estructura](ProjectStructure.md) · [Jerarquía de escenas y prefabs](SceneHierarchy.md) · [Sistemas centrales](CoreSystems.md) |
| Gameplay | [Sistemas de juego](GameplaySystems.md) · [Enemigos y jefes](EnemiesAndBosses.md) · [Portales](Portals.md) · [Máquinas de estado](StateMachines.md) |
| Entrada e interfaz | [Input](Input.md) · [Menús y HUD](UiAndMenu.md) · [Cómics](LoreComics.md) · [Tutorial](Tutorial.md) |
| Mundo y recursos | [Mundo y cámara](WorldAndCamera.md) · [Escala](WorldScale.md) · [Zona epipelágica](ZonaEpipelagica.md) · [Recursos](AssetFlow.md) · [Animación](AnimationStandards.md) · [Iluminación](ZoneLighting.md) |
| Datos y feria | [Perfil persistente](PersistentProfile.md) · [Servidor de feria](FairServer.md) · [Preparación del evento](FairEventSetupGuide.md) |
| Plataformas | [Distribución](Distribution.md) · [Port móvil](MobilePort.md) · [Build Android](AndroidBuild.md) · [Rendimiento](AndroidPerformance.md) · [Interrupciones](AndroidLifecycle.md) · [Android sin conexión](AndroidOffline.md) |
| Calidad | [Pruebas](Testing.md) · [Validación de composición](SceneCompositionValidation.md) |

Los PDF, Markdown e imágenes de [los informes originales](04-academica/README.md) conservan las entregas del curso. Pueden describir decisiones anteriores a la implementación actual.

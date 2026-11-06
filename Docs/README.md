# Documentación · Squid Ink-Pulse

> **Cinco puertas de entrada, un solo proyecto.** Ordenamos la documentación para que una persona que quiere jugar, otra que quiere comprender el código y quien revisa la entrega universitaria puedan encontrar lo que necesitan sin leer archivos que no les corresponden.

[← Volver a la portada](../README.md) · [Créditos y derechos](../LICENSING.md)

## Las cinco áreas

| Área | Para quién | Documentos de entrada |
| --- | --- | --- |
| **01 · General** | Personas que quieren conocer el proyecto | [Presentación y alcance](01-general/README.md) |
| **02 · Técnica** | Lectores de arquitectura y responsables de sistemas | [Mapa técnico](02-tecnica/README.md), [arquitectura](02-tecnica/arquitectura.md) |
| **03 · Desarrollo** | Quien modifica código, escenas o builds | [Guía de desarrollo](03-desarrollo/README.md), [validación](03-desarrollo/pruebas-y-validacion.md) |
| **04 · Académica y formativa** | Profesorado, evaluadores y estudiantes | [Contexto e informes originales](04-academica/README.md) |
| **05 · Diseño y experiencia de juego** | Jugadores, diseñadores y desarrolladores de gameplay | [GDD](05-diseno-y-experiencia-de-juego/gdd.md), [manual de juego](05-diseno-y-experiencia-de-juego/manual-del-jugador.md) |

## Rutas de lectura

- **Quiero jugar:** [manual](05-diseno-y-experiencia-de-juego/manual-del-jugador.md) → [descargas](02-tecnica/compilacion-y-distribucion.md) → [zonas y amenazas](05-diseno-y-experiencia-de-juego/zonas-y-amenazas.md).
- **Quiero entender cómo funciona:** [arquitectura C1–C3](02-tecnica/arquitectura.md) → [dominio y guardados](02-tecnica/dominio-y-persistencia.md) → [máquinas de estado](02-tecnica/estados-y-flujos.md).
- **Quiero continuar el desarrollo:** [guía de desarrollo](03-desarrollo/README.md) → [estructura y convenciones](03-desarrollo/estructura-y-contribucion.md) → [pruebas](03-desarrollo/pruebas-y-validacion.md).
- **Quiero revisar el trabajo universitario:** [contexto](04-academica/README.md) → [informes en su versión original](reports/) → [decisiones del juego](05-diseno-y-experiencia-de-juego/gdd.md).

## Qué documentación es vigente y cuál es histórica

**Vigente:** estos cinco índices, sus capítulos y las referencias de código/escenas enlazadas. La documentación de la release siempre se contrasta con las notas del release correspondiente.

**Histórica:** los PDF, Markdown y figuras de `Docs/reports/`. Los conservamos para registrar fielmente las etapas del curso; no los reescribimos para hacer coincidir sus afirmaciones con el código más reciente.

**Referencia técnica detallada existente:** los documentos especializados que ya estaban en la raíz de `Docs/` siguen disponibles y complementan los capítulos sintéticos. No se convierten automáticamente en una garantía de que el comportamiento no haya cambiado.

| Tema | Profundización |
| --- | --- |
| Arquitectura y runtime | [SoftwareArchitecture](SoftwareArchitecture.md) · [CoreSystems](CoreSystems.md) · [ProjectStructure](ProjectStructure.md) · [RuntimeHierarchyAudit](RuntimeHierarchyAudit.md) |
| Gameplay | [GameplaySystems](GameplaySystems.md) · [EnemiesAndBosses](EnemiesAndBosses.md) · [Portals](Portals.md) · [StateMachines](StateMachines.md) |
| Controles y UI | [Input](Input.md) · [UiAndMenu](UiAndMenu.md) · [LoreComics](LoreComics.md) · [Tutorial](Tutorial.md) |
| Mundo y assets | [WorldAndCamera](WorldAndCamera.md) · [WorldScale](WorldScale.md) · [AssetFlow](AssetFlow.md) · [AnimationStandards](AnimationStandards.md) · [ZoneLighting](ZoneLighting.md) |
| Datos e integración | [PersistentProfile](PersistentProfile.md) · [FairServer](FairServer.md) · [FairEventSetupGuide](FairEventSetupGuide.md) |
| Plataformas y calidad | [Distribution](Distribution.md) · [AndroidBuild](AndroidBuild.md) · [MobilePort](MobilePort.md) · [AndroidPerformance](AndroidPerformance.md) · [Testing](Testing.md) · [SceneCompositionValidation](SceneCompositionValidation.md) |

Los documentos especializados no sustituyen el acceso por áreas: están enlazados donde resultan útiles, no repetidos en una lista interminable de capítulos. La [auditoría documental](03-desarrollo/auditoria-documental.md) explica qué afirmaciones fueron corregidas y qué falta verificar.

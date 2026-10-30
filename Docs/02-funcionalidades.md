# 02 — Catálogo funcional

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** general / técnica.

## Catálogo de capacidades — revisión 2026-10-29

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

| Módulo       | Capacidad / propósito               | Caso borde o límite                          |
| ------------ | ----------------------------------- | -------------------------------------------- |
| Run          | Autoscroll, graze e Ink-Pulse       | Pausa, colisión y game over                  |
| Contenido    | Dos zonas, enemigos y bosses        | Transición por portal                        |
| Economía     | Gadgets, tienda de run y permanente | Compra y persistencia                        |
| Narrativa/UI | Comics, menús, audio y HUD          | Safe area, foco y suspensión                 |
| Distribución | Windows y Android beta              | Perfil, escena de entrada y carpeta completa |
| Feria        | Leaderboard LAN opcional            | Host ausente no debe impedir el juego local  |

Los módulos propuestos se identifican expresamente. La disponibilidad en una plataforma no se extrapola a las demás; la aceptación debe cubrir los casos borde del recorrido principal.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

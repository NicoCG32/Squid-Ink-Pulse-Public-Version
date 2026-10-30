# 06 — Integraciones y asistencia

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** técnica.

## Integraciones y límites de asistencia

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

| Servicio / integración | Finalidad / ejecución   | Contrato o límite                                   |
| ---------------------- | ----------------------- | --------------------------------------------------- |
| Unity/URP/Input System | Runtime y build         | Editor y módulos por plataforma                     |
| Archivos locales       | Progreso y semillas     | Compatibilidad, migración y ciclo de vida           |
| Feria Python/SQLite    | Leaderboard de host LAN | Complemento opcional, no servicio público universal |

Si no hay IA generativa implementada, este capítulo no la introduce. Una IA rival de videojuego no es un proveedor LLM. Cuando existe asistencia, documentar entradas enviadas, revisión, esquema, timeout/cancelación y efecto permitido. Los reintentos/fallback deben corresponder al código y al consentimiento; no se agregan a un contrato de intento único para seguir una plantilla.

Las claves de servidor y firma no se incluyen en cliente, informes o archivos de distribución. La disponibilidad, costo y condiciones de los proveedores se verifican para el entorno real. Sin esa comprobación, distinguir integración configurada, declarada y probada.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

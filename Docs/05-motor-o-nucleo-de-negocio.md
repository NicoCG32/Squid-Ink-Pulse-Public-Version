# 05 — Reglas y núcleo

## Núcleo e invariantes

- Graze y carga habilitan Ink-Pulse; no se confunden con el daño/colisión.
- Estado temporal de run, catálogo y progreso permanente tienen dueños distintos.
- Los portales conservan el contrato de sesión al cambiar de zona.
- Las operaciones de tienda/persistencia deben normalizar y conservar perfiles compatibles.
- Una APK beta Development no se presenta como Android final; la versión Windows no certifica ergonomía touch.
- El leaderboard de feria no acredita sincronización de progreso entre equipos.

### Entradas, decisión y efectos

El recorrido de [03-flujo-de-usuario.md](03-flujo-de-usuario.md) proporciona entradas al núcleo. Las reglas anteriores determinan lo aceptado y sus efectos; UI y proveedores externos no deben sustituir silenciosamente esas decisiones. El modelo de [04-base-de-datos.md](04-base-de-datos.md) identifica los estados/referencias afectados.

Los documentos especializados enlazados detallan las fórmulas, parámetros y efectos de cada operación.

### Pruebas con propósito

Para comprobar el núcleo, cubre una operación válida, una entrada rechazada y la reapertura o repetición de la operación. Los comandos de prueba están en [instalación y despliegue](09-instalacion-y-despliegue.md).

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

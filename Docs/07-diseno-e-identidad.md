# 07 — Diseño y accesibilidad

<!-- DOCUMENTACION:nivel-2026-10-29:BEGIN -->

**Nivel:** técnica / producto.

## Identidad y experiencia de uso

Revisión documental: 2026-10-29. Base `3feadba8f146` en `mobile/07-run-overlays`; 1 entradas de cambios locales conservadas al iniciar la revisión. La inspección de fuentes no sustituye pruebas de ejecución.

**Estado y alcance:** Juego base Windows cerrado y jugable según documentación, con port móvil en evolución. ReleaseSource conserva Windows estable y APK Android beta Development; la rama mobile/07-run-overlays es más avanzada y tiene trabajo local conservado.

Identidad submarina, pixel art y contraste de zonas; HUD y menús son parte del feedback de riesgo. El port móvil adapta touch, safe area y overlays sin cambiar arbitrariamente reglas del juego. Audio/animación acompañan estado, no sustituyen sus contratos.

### Criterios verificables

- Estados vacíos, carga, confirmación y error distinguibles por texto además de color.
- Orden de foco, teclado y nombres accesibles cuando la plataforma los utilice.
- Legibilidad y controles suficientes para el dispositivo objetivo; considerar área segura y ciclo de vida en móvil.
- Evitar que animación, espera o respuesta tardía apliquen efectos ajenos a la selección vigente.

Son criterios de revisión, no certificaciones de accesibilidad ya obtenidas. La aceptación se registra por resolución/plataforma y recorrido; no se extrapola desde una captura aislada.

## Fuentes de implementación y contratos

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).
<!-- DOCUMENTACION:nivel-2026-10-29:END -->

<!-- IDENTIDAD:2026-10-30 -->

## Recursos de identidad y distribución

[Catálogo de originales y variantes](../branding/README.md) · [Manifest de identidad](../branding/manifest.json).

El menú principal utiliza el lettering del pack como Sprite, conservando proporción. ProjectSettings guarda el icono de aplicación para los siguientes builds. No se sustituyen sprites de gameplay ni audio; Matías conserva los créditos y la titularidad declarada. Las releases históricas mantienen sus binarios.

Los banners de presentación se seleccionan por tema en el README. Las aplicaciones incorporan sólo los recursos necesarios, con dimensiones/proporción estables; las variantes y originales permanecen en branding. La identidad visual no amplía funcionalidades ni cambia las condiciones de uso.

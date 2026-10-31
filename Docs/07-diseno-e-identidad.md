# 07 — Diseño y accesibilidad

## Identidad y experiencia de uso

Identidad submarina, pixel art y contraste de zonas; HUD y menús son parte del feedback de riesgo. El port móvil adapta touch, safe area y overlays sin cambiar arbitrariamente reglas del juego. Audio/animación acompañan estado, no sustituyen sus contratos.

### Criterios verificables

- Estados vacíos, carga, confirmación y error distinguibles por texto además de color.
- Orden de foco, teclado y nombres accesibles cuando la plataforma los utilice.
- Legibilidad y controles suficientes para el dispositivo objetivo; considerar área segura y ciclo de vida en móvil.
- Evitar que animación, espera o respuesta tardía apliquen efectos ajenos a la selección vigente.

Comprueba estos criterios en los recorridos principales y en los tamaños de pantalla de la plataforma que vas a utilizar.

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

## Recursos de identidad y distribución

[Catálogo de originales y variantes](../branding/README.md) · [Manifest de identidad](../branding/manifest.json).

El menú principal utiliza el lettering del pack como Sprite, conservando proporción. ProjectSettings guarda el icono de aplicación para los siguientes builds. No se sustituyen sprites de gameplay ni audio; Matías conserva los créditos y la titularidad declarada. Las releases históricas mantienen sus binarios.

Los banners de presentación se seleccionan por tema en el README. Las aplicaciones incorporan sólo los recursos necesarios, con dimensiones/proporción estables; las variantes y originales permanecen en branding. La identidad visual no amplía funcionalidades ni cambia las condiciones de uso.

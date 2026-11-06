# Roadmap

El juego cuenta con una versión estable para Windows y una beta para Android. La prioridad actual es completar la adaptación móvil conservando las reglas del juego y el progreso local.

## Android

La base móvil incorpora controles táctiles, HUD y menús con área segura, persistencia local, iconos de aplicación y un objetivo de presentación de 60 FPS. El código actual añade navegación contextual con Atrás, pausa al volver de segundo plano, aislamiento del complemento de feria y compresión ASTC para las texturas de PezGlobo.

Las descargas publicadas y sus cambios se consultan en [Releases](https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases). El código más reciente puede incluir mejoras posteriores a esos binarios.

| Próximo paso | Resultado esperado |
| --- | --- |
| Ampliar el recorrido de interrupciones | Revisar ambas zonas, tiendas y cómics al cambiar de aplicación o bloquear la pantalla. |
| Revisar orientación y reapertura | Comprobar landscape en ambos sentidos, cierre del proceso y progreso tras reabrir. |
| Ampliar dispositivos y duración | Revisar teléfonos, cargas de zona, memoria y sesiones prolongadas. |
| Preparar candidata de distribución | Firma de distribución, versión, instalación limpia y actualización con datos existentes. |

[Contrato móvil y matriz de pruebas](MobilePort.md) · [Build Android](AndroidBuild.md) · [Rendimiento](AndroidPerformance.md)

## Mantenimiento

- Conservar la compatibilidad de perfiles, mejoras y compras.
- Comprobar Windows cuando cambien sistemas compartidos.
- Mantener las guías y los paquetes vinculados a su versión fuente.

## Evolución posterior

Nuevas zonas, enemigos, jefes, skins, balance y pulido audiovisual pueden ampliar la experiencia. El complemento de feria también puede evolucionar si se retoma su alcance de evento. Estas iniciativas requieren definir contenido, soporte y condiciones con el equipo.

[Proyección del proyecto](03-desarrollo/proyeccion.md) · [Créditos y derechos](../LICENSING.md)

# 01 — Arquitectura por niveles

## Arquitectura del sistema por niveles

### Nivel 1 — Contexto y fronteras de ejecución

```text
[Jugador] -> [Entrada / menús / HUD]
                       |
            [Controladores de sesión y estado]
                 |                  |
          [Enemigos / bosses]   [Ink-Pulse / graze]
                 |                  |
                 +--> [Zonas / portales] --> [Resultado]
                                            |
                                  [Perfil JSON local]

[Servidor feria opcional] -> [Leaderboard LAN del host]
[Unity build] -> [ZIP Windows / APK Android beta]
```

Endless runner 2D submarino: el graze carga Ink-Pulse y abre una ventaja temporal; zonas, enemigos, bosses, portales y economía local sostienen progresión. La base funciona sin servicios; feria es un add-on opcional y Android requiere controles/UI/ciclo de vida propios.

### Nivel 2 — Componentes y responsabilidades

| Componente | Responsabilidad                | Fuente                                                                                                                                          |
| ---------- | ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| Núcleo     | Sesión, escena y progresión    | [Assets/Implementation/Code/Core](../Assets/Implementation/Code/Core)                                                                           |
| Jugador    | Movimiento, habilidad y perfil | [Assets/Implementation/Code/Player](../Assets/Implementation/Code/Player)                                                                       |
| Perfil     | Carga/guardado y normalización | [Assets/Implementation/Code/Player/Profile/PlayerProfileRepository.cs](../Assets/Implementation/Code/Player/Profile/PlayerProfileRepository.cs) |
| UI         | HUD, menús y overlays          | [Assets/Implementation/Code/UI](../Assets/Implementation/Code/UI)                                                                               |
| Contenido  | Escenas, prefabs y recursos    | [Assets/Content](../Assets/Content)                                                                                                             |
| Feria      | Servidor local opcional        | [Tools/FairServer](../Tools/FairServer)                                                                                                         |

Las flechas muestran el flujo de información entre componentes. Las transacciones y dependencias de código se detallan en los contratos de cada módulo.

### Nivel 3 — Dominio, contratos y adaptadores

Datos iniciales en Resources/PersistentDbSeeds y progreso en Application.persistentDataPath/db. JSON local para perfil, récords y catálogo; SQLite del servidor de feria es otro almacén limitado al host. No hay una base compartida de todos los jugadores ni sincronización integral de compras.

- Graze y carga habilitan Ink-Pulse; no se confunden con el daño/colisión.
- Estado temporal de run, catálogo y progreso permanente tienen dueños distintos.
- Los portales conservan el contrato de sesión al cambiar de zona.

Consultar [04-base-de-datos.md](04-base-de-datos.md) para entidades, cardinalidades y representación; [05-motor-o-nucleo-de-negocio.md](05-motor-o-nucleo-de-negocio.md) para invariantes. Las piezas marcadas como propuestas pertenecen al diseño del prototipo.

### Nivel 4 — Despliegue y distribución

**Destino:** Windows estable + Android beta; release final condicionada a validación.

```text
[Revisión de fuente] -> [Verificación de su plataforma] -> [Build / artefacto]
                                                                  |
                                                          [Instalación / hosting]
                                                                  |
                                                          [Usuario y sus datos]
```

Para Windows, distribuye el ZIP completo, con el ejecutable y sus dependencias. Android utiliza un APK independiente; consulta su canal beta y los requisitos de instalación. Conserva la compatibilidad del perfil local al actualizar. El capítulo [09-instalacion-y-despliegue.md](09-instalacion-y-despliegue.md) concreta la puesta en marcha.

### Calidad y límites arquitectónicos

- Referencias de escena/prefab y estado al cambiar zonas
- Compatibilidad de perfiles y compras
- Memoria, rendimiento y suspensión móvil
- Derechos de recursos de equipo y mezcla entre rama de desarrollo/release

El rendimiento y la capacidad dependen del entorno de ejecución. Consulta las guías de instalación y las limitaciones de cada plataforma antes de desplegar.

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

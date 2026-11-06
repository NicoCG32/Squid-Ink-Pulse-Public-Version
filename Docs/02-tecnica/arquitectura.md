# Arquitectura del videojuego

[← Área técnica](README.md)

Documentamos la implementación que existe en el repositorio, no una arquitectura aspiracional. Unity y los componentes de sus escenas ejecutan el juego; un complemento independiente ofrece el ranking de feria.

## C1 · Contexto

```mermaid
flowchart LR
    P["Jugador"] --> G["Squid Ink-Pulse<br/>Unity · Windows / Android"]
    G --> L[("Archivos JSON<br/>en el dispositivo")]
    G -. "solo modo feria configurado" .-> S["Servidor de feria<br/>Python · LAN"]
    S --> DB[("SQLite del host")]
    V["Visitantes de feria"] --> W["Leaderboard web"]
    S --> W
```

La línea punteada indica una integración **opcional**. No representa una cuenta en la nube ni un servicio de guardado universal.

## C2 · Contenedores de ejecución

```mermaid
flowchart TB
  subgraph Dispositivo["Dispositivo del jugador"]
    Unity["Player de Unity"]
    Json[("Perfil / récords / catálogo / ranking<br/>JSON local")]
    Unity <--> Json
  end
  subgraph Feria["PC host de feria — opcional"]
    Python["Servidor HTTP<br/>Python stdlib"]
    SQLite[("SQLite")]
    Web["Página de clasificación"]
    Python <--> SQLite
    Python --> Web
  end
  Unity -. "API local de feria<br/>cuando procede" .-> Python
  Browser["Navegador en la misma LAN"] --> Web
```

**Límite operativo:** la presentación validada del complemento se centra en resultados guardados desde el PC host y visualización web en otros dispositivos. La API tiene más endpoints, pero no se debe anunciar como sincronización completa de progreso multi-PC.

## C3 · Componentes principales de Unity

```mermaid
flowchart TB
    Input["Input System<br/>lector semántico"] --> Player["Jugador<br/>movimiento / estado / habilidades"]
    Session["GameSessionController<br/>RunProgressionDirector"] --> Player
    Session --> Spawn["LevelSpawner<br/>perfiles de spawn"]
    Spawn --> Enemy["Enemigos y jefes"]
    Player --> Collision["Colisión / graze / camarones"]
    Collision --> Pulse["InkPulseController"]
    Pulse --> UI["HUD y audio"]
    Session --> UI
    Session --> Scene["SceneFlowController<br/>portales y escenas"]
    Player --> Shop["Gadgets / tiendas"]
    Shop --> Profile["Perfil y catálogo"]
    Session --> Profile
    Profile --> Json[("JSON local")]
```

Este nivel representa **responsabilidades y dependencias conceptuales**, no una lista literal de llamadas entre clases. La dirección efectiva debe contrastarse con los scripts y referencias de cada prefab/escena.

## C4 · Estructura y despliegue

| Ruta | Función |
| --- | --- |
| `Assets/Scenes/` | Escenas serializadas. |
| `Assets/Implementation/Code/` | Controladores, dominio, interfaz y adaptadores Unity en C#. |
| `Assets/Implementation/Config/` | Perfiles de configuración. |
| `Assets/Content/` | Prefabs, arte, animaciones, audio. |
| `Assets/Implementation/Resources/PersistentDbSeeds/` | Semillas JSON para nuevas instalaciones. |
| `Packages/` y `ProjectSettings/` | Dependencias y configuración del editor. |
| `Tools/FairServer/` | Add-on Python/SQLite de feria. |

**Escenas activas según `ProjectSettings/EditorBuildSettings.asset`:** `MainMenu`, `ZonaEpipelagica`, `ZonaAbisopelagica`, `ShopMenu`. `ZonaTutorial` existe, pero está excluida de compilación.

```mermaid
flowchart LR
  Src["Fuentes Unity + paquetes"] --> Editor["Unity 6000.3.11f1"]
  Editor --> Windows["ZIP Windows x64"]
  Editor --> Android["APK Android ARM64 beta"]
  Server["Tools/FairServer<br/>Python"] --> Host["PC host de feria opcional"]
```

La arquitectura detallada, componentes y contratos se desarrollan en [SoftwareArchitecture](../SoftwareArchitecture.md), [CoreSystems](../CoreSystems.md), [ProjectStructure](../ProjectStructure.md) y [Input](../Input.md).

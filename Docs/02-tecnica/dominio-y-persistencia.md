# Dominio y persistencia

[← Área técnica](README.md)

No hay una base SQL única para el juego. Separamos **datos de una partida**, **progreso permanente** y **complemento de feria**. Esta distinción evita dibujar tablas inexistentes o confundir los camarones de una partida con dinero real.

## Modelo conceptual

```mermaid
classDiagram
    class Run {
      +score
      +zona actual
      +estado de progresión
      +gadgets temporales
      +carga InkPulse
    }
    class PlayerProfile {
      +mejoras permanentes
      +skins
      +gadgets habilitados
      +comics vistos
    }
    class PlayerRecords {
      +totalShrimps
      +bestScore
      +totalRuns
      +totalPortalsCrossed
      +totalShrimpsCollected
    }
    class UnlockablesCatalog {
      +skins
      +runGadgets
      +permanentUpgrades
    }
    class LocalLeaderboard {
      +maxEntries
      +entries
    }
    class LeaderboardEntry {
      +playerName
      +score
      +zoneId
      +timestampUtc
    }
    PlayerProfile --> UnlockablesCatalog : referencia IDs de contenido
    PlayerProfile --> PlayerRecords : progreso del dispositivo
    LocalLeaderboard "1" *-- "0..*" LeaderboardEntry
    Run ..> PlayerRecords : registra resultado al terminar
    Run ..> PlayerProfile : utiliza mejoras vigentes
```

Las asociaciones anteriores son **conceptuales**. En los archivos JSON no se implementan claves foráneas SQL.

## Archivos persistidos

| Archivo en `Application.persistentDataPath/db/` | Propósito |
| --- | --- |
| `player-profile.json` | Niveles de mejoras, skins desbloqueadas y equipada, elegibilidad de gadgets y cómics vistos. |
| `player-records.json` | Moneda local (camarones), récord, número de partidas, portales y camarones recogidos. |
| `unlockables-catalog.json` | Definiciones y precios de contenido; usa una semilla suministrada con el juego. |
| `local-leaderboard.json` | Entradas de ranking local; no es el ranking de feria. |

Cada estructura contiene una versión y normalizaciones de datos. Las clases que definen la forma real son `PlayerProfileSaveData`, `PlayerRecordsSaveData`, `UnlockablesCatalogSaveData` y `LocalLeaderboardSaveData`.

## Flujo de lectura/escritura

```mermaid
sequenceDiagram
  participant U as Unity
  participant R as Repositorio de perfil
  participant F as JSON local
  participant S as Semillas Resources
  U->>R: Solicitar perfil / récords
  R->>F: Leer archivos existentes
  alt Archivo existe y es válido
    F-->>R: Datos versionados
  else Instalación nueva / archivo no disponible
    R->>S: Obtener valores iniciales
    S-->>R: Datos base
  end
  R-->>U: Datos normalizados
  U->>R: Progreso, compra o fin de run
  R->>F: Guardar resultado local
```

La implementación exacta de escritura, restauración y migración se detalla en [PersistentProfile](../PersistentProfile.md). Este esquema es de alto nivel, no una transcripción de todas las ramas de error del repositorio.

## Base SQLite del servidor de feria

```mermaid
erDiagram
  FAIR_PARTICIPANTS ||--o{ FAIR_EVENTS : "registra eventos (opcional)"
  FAIR_PARTICIPANTS {
    TEXT participant_id PK
    TEXT nickname
    TEXT recovery_code
    TEXT nickname_normalized
    TEXT active_session_machine_id
    TEXT active_session_expires_at
    INTEGER best_score
    INTEGER attempt_count
    TEXT snapshot_json
    TEXT created_at
    TEXT updated_at
  }
  FAIR_EVENTS {
    INTEGER event_id PK
    TEXT participant_id
    TEXT event_type
    TEXT machine_id
    TEXT created_at
    TEXT payload_json
  }
```

Es un **extracto de columnas relevantes** del esquema creado en `Tools/FairServer/server.py`; hay columnas adicionales que se consultan en el archivo fuente. `fair_events.participant_id` es nullable: el diagrama expresa relación lógica y **no certifica una restricción de clave foránea declarada**, ya que el SQL mostrado no la define. Los datos se almacenan en `Tools/FairServer/data/fair_server.sqlite3` del host.

No se propone un diagrama ER para los JSON locales porque **no son tablas relacionales**. Para ellos, el diagrama de clases anterior es una representación más fiel.

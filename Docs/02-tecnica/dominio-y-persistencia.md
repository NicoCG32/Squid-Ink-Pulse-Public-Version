# Dominio y persistencia

[← Área técnica](README.md)

El juego mantiene el estado temporal de la partida en memoria y el progreso permanente en archivos JSON del dispositivo. El complemento de feria utiliza una base SQLite propia en el PC anfitrión.

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

Las asociaciones muestran cómo se relacionan los datos del juego. El perfil referencia el contenido mediante sus identificadores.

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

La escritura atómica, recuperación y migración se detallan en [persistencia local](../PersistentProfile.md).

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

El diagrama resume las columnas principales de [server.py](../../Tools/FairServer/server.py). La asociación es lógica: `fair_events.participant_id` admite nulos y carece de una restricción SQL de clave foránea. El archivo se guarda en `Tools/FairServer/data/fair_server.sqlite3` del host.

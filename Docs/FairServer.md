# Servidor de feria

## Proposito

El servidor de feria es una capa externa para eventos presenciales. No reemplaza la persistencia local normal del juego y no escribe directamente los JSON internos de Unity.

Responsabilidades:

- crear participantes de feria;
- generar codigo corto de recuperacion;
- recuperar participantes por `nickname` + `recoveryCode`;
- recibir snapshots agregados de progreso;
- mantener una leaderboard compartida entre PCs;
- servir una pantalla web de ranking.

Implementacion actual:

```text
Tools/FairServer/
|- server.py
|- start_fair_server.bat
|- start_fair_server.ps1
|- smoke_test.py
`- README.md
```

El servidor usa Python estandar y SQLite. No requiere FastAPI, Node ni paquetes externos.

## Ejecutar en Windows

Desde `Tools/FairServer/`:

```powershell
.\start_fair_server.ps1
```

O doble click en:

```text
start_fair_server.bat
```

URLs:

- host local: `http://localhost:8080/`
- clientes LAN: `http://IP_DEL_HOST:8080/`

Base SQLite:

```text
Tools/FairServer/data/fair_server.sqlite3
```

## API MVP

| Metodo | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/health` | Estado del servidor. |
| `POST` | `/participants` | Crea participante y entrega `recoveryCode`. |
| `POST` | `/participants/recover` | Recupera participante por `nickname` + codigo. |
| `GET` | `/participants/{participantId}` | Devuelve snapshot del participante. |
| `PUT` | `/participants/{participantId}/snapshot` | Sincroniza progreso. |
| `GET` | `/participants/{participantId}/rank` | Devuelve posicion actual. |
| `POST` | `/participants/{participantId}/heartbeat` | Extiende sesion exclusiva. |
| `POST` | `/participants/{participantId}/checkout` | Cierra sesion y libera participante. |
| `GET` | `/leaderboard?limit=20` | Ranking JSON. |
| `GET` | `/` | Ranking web autoactualizado. |

## Snapshot

El servidor acepta datos top-level y bloques `records`/`profile` para facilitar el adaptador Unity.

Campos principales:

```json
{
  "machineId": "PC-02",
  "bestScore": 183200,
  "attemptCount": 6,
  "records": {
    "totalShrimps": 120,
    "totalShrimpsCollected": 900,
    "totalPortalsCrossed": 2
  },
  "profile": {
    "permanentUpgrades": {
      "inkPulseDurationLevel": 2,
      "inkPulseRechargeRateLevel": 1,
      "shrimpMultiplierLevel": 0,
      "scoreMultiplierLevel": 3
    },
    "skins": {
      "unlockedSkinIds": ["skin.default", "skin.sonic"],
      "equippedSkinId": "skin.sonic"
    },
    "runGadgetUnlocks": {
      "unlockedRunGadgetIds": ["gadget.shell_shield", "gadget.ink_bottle"]
    }
  },
  "unlockedEvents": []
}
```

Reglas de consolidacion:

- `bestScore`: maximo entre servidor y cliente.
- `attemptCount`: maximo recibido.
- `totalShrimpsCollected`: maximo recibido.
- `totalPortalsCrossed`: maximo recibido.
- `permanentUpgrades`: maximo por mejora.
- `skins`, `runGadgetUnlocks` y `unlockedEvents`: union de conjuntos.
- `totalShrimps`: valor del cliente activo, porque el MVP usa sesion exclusiva por participante.

## Sesion exclusiva

Cada participante tiene una sesion activa por `machineId`.

- Crear o recuperar participante marca una sesion activa.
- Si otro PC intenta recuperar el mismo participante antes de expirar la sesion, recibe `409 active_session`.
- `heartbeat` extiende el bloqueo.
- `checkout` libera la sesion.
- Si el PC cae, el bloqueo expira despues del timeout configurado.

## Prueba

Con el servidor corriendo:

```powershell
python .\smoke_test.py
```

La prueba crea un participante, sincroniza snapshot, consulta ranking, obtiene posicion y hace checkout.

## Siguiente integracion Unity

El lado Unity debe agregarse como adaptador separado bajo `Assets/Implementation/Code/Fair/`:

- `FairModeSettings`
- `FairApiClient`
- `FairParticipantSession`
- `FairProfileSnapshot`
- `FairProfileMapper`
- `FairLeaderboardService`
- `FairModeMenuManager`

El adaptador debe leer/escribir mediante servicios existentes de perfil, no editar JSON directamente desde UI.

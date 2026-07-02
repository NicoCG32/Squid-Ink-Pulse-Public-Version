# Servidor MVP de feria

Servidor LAN local para feria. Corre en un PC host con Windows, guarda datos en SQLite y expone:

- API JSON para Unity.
- Pantalla web de ranking en `http://localhost:8080/`.
- Recuperacion por `nickname` + `recoveryCode`.
- Sesion exclusiva suave por `machineId`.

No reemplaza la persistencia local normal del juego. El servidor guarda snapshots agregados de participantes de feria.

La guia completa para preparar una feria desde cero, generar build, configurar red, conectar 3 o 4 PCs y validar el flujo esta en [Docs/FairEventSetupGuide.md](../../Docs/FairEventSetupGuide.md).

## Ejecutar en Windows

Desde esta carpeta:

```powershell
.\start_fair_server.ps1
```

O doble click en:

```text
start_fair_server.bat
```

El servidor escucha en:

```text
http://0.0.0.0:8080
```

En el PC host se puede abrir:

```text
http://localhost:8080/
```

En otros PCs de la misma red, usar la IP del host:

```text
http://IP_DEL_HOST:8080/
```

La base queda en:

```text
Tools/FairServer/data/fair_server.sqlite3
```

## Endpoints MVP

```text
GET  /health
POST /participants
POST /participants/recover
GET  /participants/{participantId}
PUT  /participants/{participantId}/snapshot
GET  /participants/{participantId}/rank
POST /participants/{participantId}/heartbeat
POST /participants/{participantId}/checkout
GET  /leaderboard?limit=20
GET  /
```

## Crear participante

```json
{
  "nickname": "NICO",
  "machineId": "PC-02",
  "buildVersion": "feria-1.0"
}
```

Respuesta relevante:

```json
{
  "participantId": "uuid",
  "nickname": "NICO",
  "recoveryCode": "4821",
  "profileSnapshot": {}
}
```

## Sincronizar snapshot

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

Reglas de merge del MVP:

- `bestScore`: conserva el maximo.
- `attemptCount`: conserva el maximo recibido.
- `totalShrimpsCollected`: conserva el maximo.
- `totalPortalsCrossed`: conserva el maximo.
- `permanentUpgrades`: conserva el maximo por mejora.
- `skins`, `runGadgetUnlocks`, `unlockedEvents`: union de conjuntos.
- `totalShrimps`: reemplaza con el valor del cliente activo, porque la sesion es exclusiva.

## Prueba de humo

Con el servidor corriendo:

```powershell
python .\smoke_test.py
```

Debe imprimir:

```text
Smoke test OK
```

## Operacion de feria

- Antes de abrir feria, hacer copia de `data/fair_server.sqlite3`.
- Ejecutar el servidor en el PC host.
- Abrir `http://localhost:8080/` en una pantalla visible para ranking.
- Configurar los clientes Unity para apuntar a `http://IP_DEL_HOST:8080`.
- Si un participante queda bloqueado por sesion activa, esperar el timeout o hacer checkout desde el PC que lo tiene activo.

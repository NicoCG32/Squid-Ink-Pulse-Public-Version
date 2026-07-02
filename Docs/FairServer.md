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

Para montaje completo de feria desde cero, incluyendo build Windows, distribucion a 3 o 4 PCs, red, firewall, conexion de clientes, prueba previa y reseteo de datos, ver [FairEventSetupGuide.md](FairEventSetupGuide.md).

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

## Adaptador Unity

El lado Unity vive separado bajo `Assets/Implementation/Code/Fair/`:

- `FairModeBootstrap`: prueba `/health` contra el servidor configurado al iniciar el ejecutable; solo abre el flujo de feria si el servidor responde y el modo no fue deshabilitado.
- `FairModeSettings`: define servidor, `machineId`, version de build y argumentos de arranque.
- `FairApiClient`: cliente HTTP para crear, recuperar, sincronizar snapshot, heartbeat y checkout.
- `FairApiModels`: DTOs serializables para requests, responses y snapshot.
- `FairParticipantSession`: sesion runtime persistente entre escenas; mantiene heartbeat y ejecuta checkout al salir.
- `FairProfileMapper`: convierte entre `PersistentPlayerProfile` y el snapshot del servidor.
- `FairModeMenuManager`: interfaz simple de nick/codigo/nuevo jugador.

El adaptador no escribe JSON directamente desde UI. Para aplicar un snapshot remoto usa `PersistentPlayerProfile.ReplaceForFairMode()`, que centraliza guardado, normalizacion y eventos de perfil/records.

Flujo de arranque del `.exe`:

1. `FairModeBootstrap` consulta `GET /health` con timeout corto.
2. Si el servidor no responde, no abre interfaz de feria y el juego continua en modo local normal.
3. Si el servidor responde, crea `FairParticipantSession` y muestra `FairModeMenuManager`.
4. El jugador ingresa `nickname` + `recoveryCode`, o presiona `Nuevo jugador`.
5. Recuperar llama `POST /participants/recover`; nuevo jugador llama `POST /participants`.
6. El snapshot remoto se aplica al perfil local antes de permitir jugar.
7. Se sincroniza snapshot local mediante `PUT /participants/{participantId}/snapshot`.
8. `FairParticipantSession` mantiene heartbeat periodico.

Flujo de salida:

1. `MainMenu.Salir()` consulta `FairParticipantSession`.
2. Si hay sesion activa, crea snapshot local final.
3. Llama `POST /participants/{participantId}/checkout` con `finalSnapshot`.
4. Al terminar el checkout, cierra el juego.

Configuracion de host:

- Por defecto usa `http://localhost:8080`.
- El campo `Servidor` de la interfaz guarda la URL en `PlayerPrefs`.
- Tambien puede pasarse por argumento de linea de comandos:

```text
--fair-server=http://IP_DEL_HOST:8080
--fair-machine=PC-02
--fair-disabled
```

Tambien se aceptan `--fair-server http://IP_DEL_HOST:8080` y `--fair-machine PC-02`.

`--fair-disabled` desactiva el overlay de feria para builds o pruebas que no usen servidor.

Si no hay servidor disponible en la URL configurada, el overlay de nick/codigo no aparece. Esto evita bloquear el juego normal cuando el ejecutable se abre fuera del montaje de feria.
Si el juego se abre con `--fair-server` explicito, el overlay se muestra aunque el chequeo `/health` falle, para permitir diagnosticar una URL mal escrita desde la propia interfaz.

Cada build genera archivos auxiliares junto al `.exe`:

- `README_CLIENTE_FERIA.txt`: explica al cliente como reemplazar `<IP_DEL_HOST>` por la IPv4 del host y crear el acceso directo con `--fair-server`.
- `REINICIAR_DATOS_JUEGO.bat` y `REINICIAR_DATOS_JUEGO.ps1`: limpian la persistencia local de ese PC y recrean `Application.persistentDataPath/db/` desde las semillas incluidas en el build.

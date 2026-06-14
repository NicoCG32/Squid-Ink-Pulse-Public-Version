# Perfil persistente

## Proposito

El perfil persistente guarda el progreso permanente del jugador fuera de una run. No es una base de datos completa: es un archivo JSON versionado, local y pequeno.

Ruta runtime:

```text
Application.persistentDataPath/player-profile.json
```

Scripts:

- `Assets/Implementation/Code/Player/Profile/PlayerProfileSaveData.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerProfileRepository.cs`
- `Assets/Implementation/Code/Player/Profile/PersistentPlayerProfile.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerSkinIds.cs`

## Contrato JSON

Formato actual:

```json
{
  "version": 1,
  "wallet": {
    "totalShrimps": 0
  },
  "upgrades": {
    "inkPulseDurationLevel": 0,
    "inkPulseRechargeRateLevel": 0
  },
  "skins": {
    "unlockedSkinIds": [
      "skin.default"
    ],
    "equippedSkinId": "skin.default"
  },
  "stats": {
    "bestScore": 0,
    "totalRuns": 0,
    "totalPortalsCrossed": 0,
    "totalShrimpsCollected": 0
  }
}
```

## Skin default

La skin base tiene id:

```text
skin.default
```

Este id vive en `PlayerSkinIds.Default`. El JSON guarda ids, no referencias a sprites, animators ni assets. Las referencias visuales futuras deben vivir en catalogos o assets de configuracion.

## Reglas

- `settings` no pertenece a este archivo; opciones de volumen, brillo, pantalla o dificultad deben vivir en otro almacenamiento.
- `wallet.totalShrimps` es el saldo permanente usado por HUD, tienda in-run y futura tienda out-of-game.
- `upgrades` guarda niveles comprados, no valores finales. Los valores reales deben venir de un catalogo de mejoras.
- `skins.unlockedSkinIds` y `skins.equippedSkinId` guardan ids estables.
- `stats.bestScore` se actualiza al entrar en `GameOver`.
- `stats.totalPortalsCrossed` se actualiza cuando un portal acepta cargar la escena destino.
- `stats.totalShrimpsCollected` aumenta solo al recolectar camarones reales; reembolsos de tienda no inflan esta estadistica.
- `PlayerProfileRepository` guarda con archivo temporal y reemplazo del JSON real.

## Relacion con ShrimpRuntimeWallet

`ShrimpRuntimeWallet` sigue siendo la API que usan HUD y tienda in-run. Internamente carga y guarda su saldo mediante `PersistentPlayerProfile`.

Esto mantiene estable el codigo existente mientras permite que el saldo sobreviva al cierre del juego.

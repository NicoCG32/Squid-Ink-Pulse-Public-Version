# Persistencia local

## Proposito

La persistencia permanente se organiza como una pequena base JSON local. No es una base de datos remota: son archivos versionados, legibles y reemplazados de forma atomica cuando se guardan.

Hay dos ubicaciones:

```text
Assets/StreamingAssets/db/
Application.persistentDataPath/db/
```

`StreamingAssets/db` contiene semillas incluidas en el build. `persistentDataPath/db` contiene los archivos reales de guardado durante runtime. En un build, no se debe escribir dentro de `StreamingAssets`.

## Archivos

| Archivo | Tipo | Responsabilidad |
| --- | --- | --- |
| `unlockables-catalog.json` | Catalogo de contenido | Define skins, gadgets de run desbloqueables por hitos y mejoras permanentes, junto a precio base, efecto y meta de desbloqueo. |
| `player-profile.json` | Perfil del jugador | Guarda mejoras permanentes compradas, skins desbloqueadas/equipada y gadgets de run habilitados para aparecer en la tienda in-game. |
| `player-records.json` | Economia y records | Guarda camarones, mejor puntaje, runs, portales cruzados y camarones recolectados historicamente. |
| `local-leaderboard.json` | Feria/local | Guarda ranking local ordenado por puntaje para sesiones de prueba presenciales. |

## Scripts

- `Assets/Implementation/Code/Player/Profile/PersistentDbPaths.cs`
- `Assets/Implementation/Code/Player/Profile/JsonSaveFile.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerProfileRepository.cs`
- `Assets/Implementation/Code/Player/Profile/PersistentPlayerProfile.cs`
- `Assets/Implementation/Code/Player/Profile/LocalLeaderboardRepository.cs`
- `Assets/Implementation/Code/Player/Profile/UnlockablesCatalogSaveData.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerProfileSaveData.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerRecordsSaveData.cs`
- `Assets/Implementation/Code/Player/Profile/LocalLeaderboardSaveData.cs`
- `Assets/Implementation/Code/Player/Profile/PlayerSkinIds.cs`
- `Assets/Implementation/Code/Player/Profile/UnlockablesCatalogQuery.cs`
- `Assets/Implementation/Code/Player/Profile/RunGadgetUnlockService.cs`
- `Assets/Implementation/Code/Player/Profile/PermanentShopService.cs`
- `Assets/Implementation/Code/Player/Profile/PermanentShopPurchaseResult.cs`
- `Assets/Implementation/Code/Player/Profile/PermanentUpgradeEffectResolver.cs`

## Contratos JSON

`unlockables-catalog.json`:

```json
{
  "version": 1,
  "skins": [
    {
      "id": "skin.default",
      "displayName": "Default",
      "defaultUnlocked": true,
      "basePrice": 0,
      "unlockGoal": { "goalType": "None", "targetValue": 0 }
    }
  ],
  "runGadgets": [
    {
      "id": "gadget.shell_shield",
      "displayName": "Shell Shield",
      "defaultUnlocked": true,
      "basePrice": 0,
      "unlockGoal": { "goalType": "None", "targetValue": 0 },
      "gameplayId": "ShellShield"
    }
  ],
  "permanentUpgrades": [
    {
      "id": "upgrade.ink_pulse_duration",
      "displayName": "Ink Pulse Duration",
      "defaultUnlocked": true,
      "basePrice": 100,
      "unlockGoal": { "goalType": "None", "targetValue": 0 },
      "maxLevel": 5,
      "priceGrowthMultiplier": 1.5,
      "effectPerLevel": 0.15
    }
  ]
}
```

`player-profile.json`:

```json
{
  "version": 3,
  "permanentUpgrades": {
    "inkPulseDurationLevel": 0,
    "inkPulseRechargeRateLevel": 0,
    "shrimpMultiplierLevel": 0,
    "scoreMultiplierLevel": 0
  },
  "skins": {
    "unlockedSkinIds": ["skin.default"],
    "equippedSkinId": "skin.default"
  },
  "runGadgetUnlocks": {
    "unlockedRunGadgetIds": ["gadget.shell_shield", "gadget.ink_bottle"]
  }
}
```

`player-records.json`:

```json
{
  "version": 1,
  "totalShrimps": 0,
  "bestScore": 0,
  "totalRuns": 0,
  "totalPortalsCrossed": 0,
  "totalShrimpsCollected": 0
}
```

`local-leaderboard.json`:

```json
{
  "version": 1,
  "maxEntries": 20,
  "entries": [
    {
      "playerName": "Player",
      "score": 1000,
      "zoneId": "ZonaEpipelagica",
      "timestampUtc": "2026-06-14T00:00:00.0000000Z"
    }
  ]
}
```

## Reglas

- `settings` no pertenece a estos archivos; volumen, pantalla, brillo y dificultad deben vivir en otro almacenamiento.
- El juego no escribe directamente JSON: usa `PersistentPlayerProfile`, `ShrimpRuntimeWallet` o `LocalLeaderboardRepository`.
- `PlayerProfileRepository` es el unico punto que conoce rutas, semillas y reemplazo atomico.
- `unlockables-catalog.json` guarda definiciones y metas; no guarda si el jugador compro algo.
- `player-profile.json` guarda decisiones del jugador: mejoras permanentes, skins y gadgets de run ya habilitados por hitos.
- `player-records.json` guarda valores numericos acumulados: camarones, mejor puntaje y estadisticas.
- `local-leaderboard.json` es local por dispositivo; para feria debe poder limpiarse entre sesiones si se requiere.
- La skin default siempre debe existir como `skin.default`.
- Los gadgets son exclusivos de la tienda in-game. La persistencia no guarda compras de una run; solo guarda si un gadget ya puede aparecer en la aleatoriedad de `InGameShopManager`.
- La tienda out-of-game no vende gadgets. Vende skins y mejoras permanentes.
- Las mejoras permanentes actuales son `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.
- `effectPerLevel` expresa el aumento multiplicativo por nivel: multiplicador final = `1 + nivel * effectPerLevel`.
- Los gadgets actuales quedan habilitados por defecto para no romper la tienda in-run actual.

## Separacion conceptual

| Concepto | Donde vive | Que modifica |
| --- | --- | --- |
| Gadgets de run | `runGadgets` en catalogo y `runGadgetUnlocks` en perfil | Elegibilidad para aparecer en la tienda temporal de la run. |
| Compras de gadgets durante la run | `RuntimeGadgetInventory` | Inventario temporal, se conserva entre portales y se reinicia en Game Over. |
| Skins | `skins` en catalogo y perfil | Visual activo del jugador, sin modificar reglas mecanicas. |
| Mejoras permanentes | `permanentUpgrades` en catalogo y perfil | Multiplicadores permanentes de Ink-Pulse, camarones y score. |
| Camarones y records | `player-records.json` | Economia total, mejor puntaje y estadisticas historicas. |

## Servicios de dominio

- `UnlockablesCatalogQuery`: consulta definiciones, calcula precios y evalua metas contra `PlayerRecordsSaveData`.
- `RunGadgetUnlockService`: revisa hitos y habilita gadgets de run en `player-profile.json`; `InGameShopManager` lo usa para filtrar ofertas posibles.
- `PermanentShopService`: servicio transaccional para `ShopMenu`; valida metas, saldo, nivel maximo, propiedad previa y descuenta camarones.
- `PermanentUpgradeEffectResolver`: traduce niveles persistidos en multiplicadores usados por gameplay.

La UI no debe duplicar estas reglas. Un boton de tienda permanente debe llamar a `PermanentShopService`; una tienda in-game debe seguir pasando por `InGameShopManager`.

## Migracion

Si existe el formato antiguo:

```text
Application.persistentDataPath/player-profile.json
```

`PlayerProfileRepository` lo migra automaticamente cuando faltan los nuevos archivos en `Application.persistentDataPath/db/`.

Mapeo desde formato monolitico legacy:
- `wallet.totalShrimps` pasa a `player-records.json.totalShrimps`.
- `stats.bestScore`, `totalRuns`, `totalPortalsCrossed` y `totalShrimpsCollected` pasan a `player-records.json`.
- `upgrades` y `skins` pasan a `player-profile.json`.

Mapeo desde `player-profile.json` version 2:
- `upgrades` pasa a `permanentUpgrades`.
- `gadgets.unlockedGadgetIds` pasa a `runGadgetUnlocks.unlockedRunGadgetIds`.
- `activeSkillIds` se descarta porque las skills persistentes aun no son un contrato vivo. Las mejoras permanentes ocupan ese rol mediante ids de upgrade y niveles.

## Validacion

`Tools/Squid/Validate Scene Contracts` valida que existan y puedan parsearse las semillas de:

- `Assets/StreamingAssets/db/unlockables-catalog.json`
- `Assets/StreamingAssets/db/player-profile.json`
- `Assets/StreamingAssets/db/player-records.json`
- `Assets/StreamingAssets/db/local-leaderboard.json`

Tambien valida que el catalogo contenga `skin.default`, `gadget.shell_shield`, `gadget.ink_bottle`, `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.

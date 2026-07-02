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
| `player-profile.json` | Perfil del jugador | Guarda mejoras permanentes compradas, skins desbloqueadas/equipada, gadgets de run habilitados para aparecer en la tienda in-game y comics de lore ya vistos. |
| `player-records.json` | Economia y records | Guarda camarones, mejor puntaje, runs, portales cruzados y camarones recolectados historicamente. |
| `local-leaderboard.json` | Local por dispositivo | Guarda ranking local ordenado por puntaje. Es historico/fallback y no es la fuente compartida para una feria multi-PC. |

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
- `Assets/Implementation/Code/Player/Visual/PlayerSkinApplier.cs`
- `Assets/Implementation/Code/Player/Visual/PlayerSkinVisualSet.cs`
- `Assets/Implementation/Code/UI/Shop/OutOfGameShopManager.cs`

## Contratos JSON

`unlockables-catalog.json`:

```json
{
  "version": 8,
  "skins": [
    {
      "id": "skin.default",
      "displayName": "Default",
      "description": "",
      "defaultUnlocked": true,
      "basePrice": 0,
      "shopSpriteResourcePath": "",
      "shopHighlightedSpriteResourcePath": "",
      "shopBuyedSpriteResourcePath": "",
      "shopSelectedSpriteResourcePath": "",
      "playerSkinPrefabResourcePath": "PlayerSkins/Default",
      "unlockGoal": { "goalType": "None", "targetValue": 0 }
    }
  ],
  "runGadgets": [
    {
      "id": "gadget.shell_shield",
      "displayName": "Shell Shield",
      "description": "",
      "defaultUnlocked": true,
      "basePrice": 0,
      "shopSpriteResourcePath": "",
      "shopHighlightedSpriteResourcePath": "",
      "unlockGoal": { "goalType": "None", "targetValue": 0 },
      "gameplayId": "ShellShield"
    }
  ],
  "permanentUpgrades": [
    {
      "id": "upgrade.ink_pulse_duration",
      "displayName": "Tinta Persistente",
      "description": "Tu nube aguanta mas: entra, limpia el peligro y sal con estilo.",
      "defaultUnlocked": true,
      "basePrice": 100,
      "shopSpriteResourcePath": "ShopMenu/Skills/Upgrades/InkPulsePersistence",
      "shopHighlightedSpriteResourcePath": "ShopMenu/Skills/Upgrades/InkPulsePersistenceInk",
      "unlockGoal": { "goalType": "None", "targetValue": 0 },
      "maxLevel": 10,
      "priceGrowthMultiplier": 1.5,
      "effectMode": "Multiplier",
      "baseEffectValue": 1,
      "effectPerLevel": 0.075
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
  },
  "lore": {
    "viewedComicEventIds": []
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
  "entries": []
}
```

## Estado limpio actual

La semilla vigente del proyecto arranca sin progreso adquirido:

- mejoras permanentes en nivel `0`;
- camarones `0`;
- best score `0`;
- runs `0`;
- portales cruzados `0`;
- camarones historicos recolectados `0`;
- leaderboard local vacio;
- solo `skin.default` desbloqueada y equipada.

La skin default no se considera una compra: es el fallback tecnico minimo para que el jugador siempre tenga un visual valido. Por eso, "sin skins" significa sin skins adicionales compradas.

Los gadgets `gadget.shell_shield` y `gadget.ink_bottle` siguen habilitados por defecto en `runGadgetUnlocks` porque el flujo actual de tienda in-game depende de que puedan aparecer durante la run. No representan compras permanentes de ShopMenu.

## Reglas

- `settings` no pertenece a estos archivos; volumen, pantalla, brillo y dificultad deben vivir en otro almacenamiento.
- El juego no escribe directamente JSON: usa `PersistentPlayerProfile`, `ShrimpRuntimeWallet` o `LocalLeaderboardRepository`.
- `PlayerProfileRepository` es el unico punto que conoce rutas, semillas y reemplazo atomico.
- `unlockables-catalog.json` guarda definiciones y metas; no guarda si el jugador compro algo.
- `player-profile.json` guarda decisiones del jugador: mejoras permanentes, skins y gadgets de run ya habilitados por hitos.
- `player-profile.json/lore.viewedComicEventIds` guarda comics de portal y tienda in-game ya vistos para no repetirlos en partidas futuras.
- `player-records.json` guarda valores numericos acumulados: camarones, mejor puntaje y estadisticas.
- `local-leaderboard.json` es local por dispositivo. El modo feria multi-PC usa el servidor LAN descrito en `Docs/FairServer.md` y `Docs/FairEventSetupGuide.md`.
- La skin default siempre debe existir como `skin.default`.
- El catalogo runtime solo incluye skins con prefab jugable disponible; skins conceptuales sin animacion/prefab se mantienen en fuentes de diseno hasta implementarse.
- Los gadgets son exclusivos de la tienda in-game. La persistencia no guarda compras de una run; solo guarda si un gadget ya puede aparecer en la aleatoriedad de `InGameShopManager`.
- La tienda out-of-game no vende gadgets. Vende skins y mejoras permanentes.
- Las mejoras permanentes actuales son `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.
- El copy de mejoras debe ser breve, legible en el mueble de tienda y sin tildes ni caracteres especiales. Textos actuales:
  - `upgrade.ink_pulse_duration`: "Tu nube aguanta mas: entra, limpia el peligro y sal con estilo."
  - `upgrade.ink_pulse_recharge_rate`: "Menos espera entre pulsos; mas escapes al limite."
  - `upgrade.shrimp_multiplier`: "Cada camaron rinde mas cuando el oceano se pone pesado."
  - `upgrade.score_multiplier`: "Cada maniobra peligrosa deja una historia mas grande."
- Las mejoras permanentes tienen tope de nivel definido por `maxLevel`; el catalogo actual usa `10`.
- `priceGrowthMultiplier` hace que el precio de cada siguiente nivel crezca a partir del precio base y del nivel actual.
- `effectMode` define como se aplica el efecto: `Multiplier` para multiplicadores y `Additive` para incrementos absolutos.
- En `Multiplier`, el valor final se calcula desde `baseEffectValue + nivel * effectPerLevel`.
- En `Additive`, el valor final se suma como incremento permanente al sistema consumidor.
- Los gadgets actuales quedan habilitados por defecto para no romper la tienda in-run actual.
- Las rutas `shopSpriteResourcePath`, `shopHighlightedSpriteResourcePath`, `shopBuyedSpriteResourcePath` y `shopSelectedSpriteResourcePath` son rutas de `Resources` sin extension hacia sprites de tienda.
- `playerSkinPrefabResourcePath` es una ruta de `Resources` sin extension hacia el prefab visual jugable de la skin. La skin default debe apuntar a `PlayerSkins/Default`; si otra skin deja esta ruta vacia, se puede comprar y equipar a nivel de perfil, pero el jugador conserva el visual base.
- El prefab apuntado por `playerSkinPrefabResourcePath` debe contener `MovementVisual` o `SquidVisual`, `InkPulseVisual` y `PortalVisual`. Estas tres raices permiten que cada skin tenga animacion propia de movimiento, portal e Ink-Pulse.
- Las skins activas del catalogo actual son `skin.default`, `skin.bob_marley`, `skin.rockstar`, `skin.formal`, `skin.sonic`, `skin.huaso`, `skin.chile`, `skin.nemo` y `skin.travis`.

## Separacion conceptual

| Concepto | Donde vive | Que modifica |
| --- | --- | --- |
| Gadgets de run | `runGadgets` en catalogo y `runGadgetUnlocks` en perfil | Elegibilidad para aparecer en la tienda temporal de la run. |
| Compras de gadgets durante la run | `RuntimeGadgetInventory` | Inventario temporal, se conserva entre portales y se reinicia en Game Over. |
| Skins | `skins` en catalogo y perfil | Compra y eleccion del visual activo del jugador, sin modificar reglas mecanicas. |
| Mejoras permanentes | `permanentUpgrades` en catalogo y perfil | Multiplicadores permanentes de Ink-Pulse, camarones y score. |
| Comics vistos | `lore.viewedComicEventIds` en perfil | Omision persistente de comics de portal y tienda in-game ya presentados. |
| Camarones y records | `player-records.json` | Economia total, mejor puntaje y estadisticas historicas. |

## Servicios de dominio

- `UnlockablesCatalogQuery`: consulta definiciones, calcula precios y evalua metas contra `PlayerRecordsSaveData`.
- `RunGadgetUnlockService`: revisa hitos y habilita gadgets de run en `player-profile.json`; `InGameShopManager` lo usa para filtrar ofertas posibles.
- `PermanentShopService`: servicio transaccional para `ShopMenu`; valida metas, saldo, nivel maximo, propiedad previa, equipamiento de skins y descuenta camarones.
- `PermanentUpgradeEffectResolver`: traduce niveles persistidos en multiplicadores usados por gameplay.

La UI no debe duplicar estas reglas. Un boton de tienda permanente debe llamar a `PermanentShopService`; una tienda in-game debe seguir pasando por `InGameShopManager`.

`OutOfGameShopManager` es la fachada de `ShopMenu`: serializa botones de seleccion y compra, consulta el catalogo para poblar estados y delega la transaccion a `PermanentShopService`. No escribe JSON, no mantiene una wallet propia, no recalcula precios y no crea elementos visuales en runtime.

Comportamiento de compra permanente:
- Comprar una mejora sube exactamente un nivel, descuenta camarones de `ShrimpRuntimeWallet` y persiste el nivel en `player-profile.json`.
- Comprar una skin no poseida descuenta camarones y agrega su id a `skins.unlockedSkinIds`.
- Comprar/usar una skin ya poseida intenta equiparla y actualiza `skins.equippedSkinId`.
- Comprar/usar una skin ya equipada, siempre que no sea `skin.default`, la deselecciona y vuelve a equipar `skin.default`.
- Cuando un jugador equipado entra a gameplay, `PlayerSkinApplier` consulta `equippedSkinId`, carga `playerSkinPrefabResourcePath`, instancia el prefab bajo `BabySquid/SkinMount` y entrega sus tres visuales a `PlayerVisualStateController`.
- La compra no debe guardarse dentro de `StreamingAssets`; durante runtime se escribe en `Application.persistentDataPath/db/`.
- Para pruebas limpias en Editor, borrar o reemplazar `Application.persistentDataPath/db/` restaura la semilla incluida en `Assets/StreamingAssets/db/`.

## Reinicio local controlado

En Windows, con la configuracion actual de `ProjectSettings`, `Application.persistentDataPath` resuelve a:

```text
C:\Users\<usuario>\AppData\LocalLow\DefaultCompany\Squid Ink-Pulse
```

El proyecto incluye un script para hacer este reinicio sin tocar las semillas del repositorio:

```powershell
.\Tools\CleanPersistentData.ps1
```

Tambien existe un wrapper para doble click:

```text
Tools/CleanPersistentData.bat
```

Ademas, cada build de Unity genera scripts equivalentes junto al `.exe`:

```text
REINICIAR_DATOS_JUEGO.bat
REINICIAR_DATOS_JUEGO.ps1
```

Estos scripts son para PCs cliente fuera del repo. Toman las semillas limpias desde `<NombreDelJuego>_Data/StreamingAssets/db/` y limpian solo la persistencia local de ese usuario Windows.

Comportamiento por defecto:

- respalda la persistencia actual en `Application.persistentDataPath/_backups/clean-YYYYMMDD-HHMMSS`;
- borra `Application.persistentDataPath/db/`;
- borra el legacy `Application.persistentDataPath/player-profile.json` si existe;
- recrea `Application.persistentDataPath/db/` copiando los JSON limpios desde `Assets/StreamingAssets/db/`;
- no borra `PlayerPrefs`, por lo que opciones de pantalla/volumen y URL de feria se conservan.

Opciones utiles:

```powershell
.\Tools\CleanPersistentData.ps1 -NoBackup
.\Tools\CleanPersistentData.ps1 -IncludePlayerPrefs
.\Tools\CleanPersistentData.ps1 -WhatIf
```

`-IncludePlayerPrefs` tambien borra la clave `HKCU\Software\DefaultCompany\Squid Ink-Pulse`, por lo que debe usarse solo si se quiere limpiar opciones y residuos externos al progreso.

Para reiniciar el progreso local sin tocar las semillas del repositorio:

1. Cerrar Play Mode o el build.
2. Ejecutar `Tools/CleanPersistentData.bat` o `.\Tools\CleanPersistentData.ps1`.
3. Abrir nuevamente Play Mode o el build.

Reinicio aplicado el 2026-06-30 en el equipo de trabajo:

```text
C:\Users\cythg\AppData\LocalLow\DefaultCompany\Squid Ink-Pulse\db
```

Resultado verificado:

- niveles de mejoras `0,0,0,0`;
- `skin.default` como unica skin desbloqueada y equipada;
- `totalShrimps = 0`;
- `bestScore = 0`;
- `totalRuns = 0`;
- leaderboard vacio;
- catalogo version `8` con `9` skins implementadas.

## Compatibilidad De Datos

Si existe el formato antiguo:

```text
Application.persistentDataPath/player-profile.json
```

`PlayerProfileRepository` lo convierte automaticamente cuando faltan los nuevos archivos en `Application.persistentDataPath/db/`.

Mapeo desde formato monolitico legacy:
- `wallet.totalShrimps` pasa a `player-records.json.totalShrimps`.
- `stats.bestScore`, `totalRuns`, `totalPortalsCrossed` y `totalShrimpsCollected` pasan a `player-records.json`.
- `upgrades` y `skins` pasan a `player-profile.json`.

Mapeo desde `player-profile.json` version 2:
- `upgrades` pasa a `permanentUpgrades`.
- `gadgets.unlockedGadgetIds` pasa a `runGadgetUnlocks.unlockedRunGadgetIds`.
- `activeSkillIds` se descarta porque las skills persistentes no forman parte del contrato de entrega. Las mejoras permanentes ocupan ese rol mediante ids de upgrade y niveles.

## Validacion

La revision de persistencia debe confirmar que existan y puedan parsearse las semillas de:

- `Assets/StreamingAssets/db/unlockables-catalog.json`
- `Assets/StreamingAssets/db/player-profile.json`
- `Assets/StreamingAssets/db/player-records.json`
- `Assets/StreamingAssets/db/local-leaderboard.json`

Tambien valida que el catalogo contenga `skin.default`, `gadget.shell_shield`, `gadget.ink_bottle`, `upgrade.ink_pulse_duration`, `upgrade.ink_pulse_recharge_rate`, `upgrade.shrimp_multiplier` y `upgrade.score_multiplier`.

Tambien valida que `ShopMenu` tenga un `OutOfGameShopManager`, una instancia del prefab `ShrimpCounter`, los cuatro ids de upgrade canonicos y los listeners persistentes de compra, seleccion y paginacion.

# 04 — Dominio, entidades y persistencia

## Dominio y representación de datos

### Entidades y conceptos

| Entidad / concepto    | Identidad y significado             | Representación / estado            |
| --------------------- | ----------------------------------- | ---------------------------------- |
| Sesión/Run            | Estado temporal, score y progresión | Memoria de partida                 |
| Jugador runtime       | Movimiento, carga y gadgets de run  | No confundir con perfil permanente |
| PlayerProfile         | Economía, mejoras y desbloqueos     | JSON local                         |
| Records / Leaderboard | Mejores resultados                  | Local; feria separada              |
| Catálogo              | Definiciones y oferta permanente    | Semillas Resources y datos         |
| Zona/Portal           | Contexto y transición de escena     | Escenas/estado de sesión           |

### Relaciones de dominio

```text
[Jugador] -- ejecuta --> [Run]
[Run] -- produce --> [Resultado]
[Resultado] -- actualiza --> [Records]
[Perfil] -- posee --> [Mejoras/skins]
[Catálogo] -- define --> [Ofertas]
[Zona] -- conecta por --> [Portal]
```

| Origen    | Cardinalidad / relación | Destino       | Semántica                          |
| --------- | ----------------------- | ------------- | ---------------------------------- |
| Jugador   | ejecuta                 | Run           | Estado temporal                    |
| Run       | produce                 | Resultado     | Puntuación y transición a derrota  |
| Resultado | actualiza               | Records       | No sincronización global implícita |
| Perfil    | posee                   | Mejoras/skins | Persistencia permanente            |
| Catálogo  | define                  | Ofertas       | La definición no es una compra     |
| Zona      | conecta por             | Portal        | Cambio de contexto runtime         |

### Persistencia y garantías

Datos iniciales en Resources/PersistentDbSeeds y progreso en Application.persistentDataPath/db. JSON local para perfil, récords y catálogo; SQLite del servidor de feria es otro almacén limitado al host. No hay una base compartida de todos los jugadores ni sincronización integral de compras.

El diagrama describe relaciones del dominio. Los adaptadores y el esquema de persistencia determinan cómo se mantienen esas relaciones al guardar o recuperar datos.

### Consistencia y evolución

- Graze y carga habilitan Ink-Pulse; no se confunden con el daño/colisión.
- Estado temporal de run, catálogo y progreso permanente tienen dueños distintos.
- Los portales conservan el contrato de sesión al cambiar de zona.
- Las operaciones de tienda/persistencia deben normalizar y conservar perfiles compatibles.
- Una APK beta Development no se presenta como Android final; la versión Windows no certifica ergonomía touch.
- El leaderboard de feria no acredita sincronización de progreso entre equipos.

Antes de cambiar el formato de almacenamiento, comprueba la migración y la restauración con una copia de prueba. Conserva la versión del respaldo y verifica su compatibilidad con la aplicación que lo abrirá.

## Código y documentación relacionada

[README.md](../README.md) · [Docs/README.md](README.md) · [Docs/SoftwareArchitecture.md](SoftwareArchitecture.md) · [Docs/CoreSystems.md](CoreSystems.md) · [Docs/GameplaySystems.md](GameplaySystems.md) · [Docs/Testing.md](Testing.md) · [Docs/AndroidBuild.md](AndroidBuild.md).

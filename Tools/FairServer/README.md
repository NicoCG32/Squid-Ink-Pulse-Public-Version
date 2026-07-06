# Servidor local de feria

Este servidor es el add-on de feria que desarrollamos para presentaciones presenciales de Squid Ink-Pulse. Corre en un PC host, guarda datos en SQLite y muestra un leaderboard web para la red local.

El juego principal funciona sin este servidor. El alcance confiable del add-on es el leaderboard almacenado en el host. Los resultados que se guardan formalmente son los jugados desde ese PC host; otros dispositivos solo visualizan el ranking web. No presentamos como logro final la sincronización completa de progreso, compras, skins o recuperación integral entre PCs.

## Ejecutar

Desde esta carpeta:

```powershell
.\start_fair_server.ps1
```

O con doble click:

```text
start_fair_server.bat
```

El servidor escucha en:

```text
http://0.0.0.0:8080
```

En el PC host:

```text
http://localhost:8080/
http://localhost:8080/health
```

En otros PCs o celulares de la misma red, solo para visualizar el leaderboard:

```text
http://IP_DEL_HOST:8080/
```

La base queda en:

```text
Tools/FairServer/data/fair_server.sqlite3
```

Esa base vive solo en el host. Si se quiere conservar el resultado de una feria, se debe respaldar ese archivo.

## Probar en feria

Para registrar resultados, levantar el servidor y ejecutar el juego en el mismo PC host. Para ver el ranking desde otros dispositivos, abrir en navegador:

```text
http://IP_DEL_HOST:8080/
```

Si el servidor no está activo, Unity o el build pueden mostrar warnings rojos por falta de host. Para jugar local normal esos warnings se ignoran; para probar feria, primero hay que levantar este servidor.

## Endpoints técnicos

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

Los endpoints de participante y snapshot existen como base técnica del add-on, pero la entrega final debe presentarse como leaderboard host, no como persistencia remota completa entre PCs.

## Prueba de humo

Con el servidor corriendo:

```powershell
python .\smoke_test.py
```

Debe imprimir:

```text
Smoke test OK
```

Esta prueba confirma que el servidor responde. No cambia el alcance final documentado.

# Add-on de feria: servidor local

## Proposito

Como equipo tambien implementamos un add-on de feria para apoyar demostraciones presenciales. Este componente es externo al juego principal: el build normal funciona sin servidor, sin red local y sin configuración adicional.

El add-on permite levantar un servidor Python en un PC host, guardar datos en SQLite y mostrar una pagina web de leaderboard para la red local. Su alcance final debe leerse con precision:

- Logramos un servidor local en `Tools/FairServer/`.
- Logramos una base SQLite en el PC host.
- Logramos una pantalla web de ranking en `http://localhost:8080/`.
- El resultado confiable de feria es el leaderboard almacenado y mostrado desde el PC host.
- Los dispositivos ajenos al host pueden visualizar ese leaderboard web desde `http://IP_DEL_HOST:8080/`.
- Los resultados que se guardan formalmente son los jugados en el PC host.
- No cerramos como funcionalidad final la sincronizacion completa de progreso, compras, skins, mejoras o recuperacion integral de jugadores entre PCs.

Por tanto, la persistencia principal del juego sigue siendo local por dispositivo. El servidor de feria es un complemento operativo, no el sistema oficial de guardado remoto del juego.

## Estructura

```text
Tools/FairServer/
|- server.py
|- start_fair_server.bat
|- start_fair_server.ps1
|- smoke_test.py
`- README.md
```

El servidor usa Python estándar y SQLite. No requiere FastAPI, Node ni paquetes externos.

La guia operativa para preparar una prueba de feria esta en [FairEventSetupGuide.md](FairEventSetupGuide.md).

## Warnings esperados sin host

Si Unity o el build se abren sin servidor de feria activo, pueden aparecer warnings rojos asociados a la falta de host, `localhost:8080` o una conexión rechazada. Para probar el juego principal esos warnings se ignoran, siempre que no existan errores de compilación C# ni referencias rotas de escena.

Solo deben investigarse cuando el objetivo sea probar explicitamente el add-on de feria. En ese caso, primero se levanta el servidor en el host y luego se comprueba el leaderboard web.

## Ejecutar en Windows

Desde `Tools/FairServer/`:

```powershell
.\start_fair_server.ps1
```

O con doble click:

```text
start_fair_server.bat
```

URLs utiles:

- Host local: `http://localhost:8080/`
- Health check: `http://localhost:8080/health`
- Visualizacion LAN: `http://IP_DEL_HOST:8080/`

Base SQLite del evento:

```text
Tools/FairServer/data/fair_server.sqlite3
```

Este archivo queda en el PC host. Si se borra, se elimina el leaderboard y los datos registrados por ese servidor.

## API implementada

El servidor conserva endpoints para participantes, snapshot, rank, heartbeat, checkout y leaderboard:

| Metodo | Ruta | Uso técnico |
| --- | --- | --- |
| `GET` | `/health` | Estado del servidor. |
| `POST` | `/participants` | Crea participante. |
| `POST` | `/participants/recover` | Intenta recuperar participante por `nickname` + código. |
| `GET` | `/participants/{participantId}` | Devuelve datos guardados para ese participante. |
| `PUT` | `/participants/{participantId}/snapshot` | Recibe snapshot desde una instancia del juego. |
| `GET` | `/participants/{participantId}/rank` | Devuelve posición actual. |
| `POST` | `/participants/{participantId}/heartbeat` | Extiende sesión activa. |
| `POST` | `/participants/{participantId}/checkout` | Cierra sesión. |
| `GET` | `/leaderboard?limit=20` | Ranking JSON. |
| `GET` | `/` | Ranking web autoactualizado. |

Estos endpoints existen porque desarrollamos una base técnica para feria. Sin embargo, el alcance cerrado para la entrega no es la persistencia remota completa de perfil. El cierre formal del add-on se centra en que el host registre y muestre el leaderboard.

## Probar el add-on

1. Levantar el servidor en el PC host.
2. Verificar en el host:

```text
http://localhost:8080/health
```

3. Abrir el ranking:

```text
http://localhost:8080/
```

4. Ejecutar el juego en el mismo PC host para guardar resultados en la base del evento.
5. Obtener la IPv4 del host con `ipconfig` si se quiere mostrar el ranking en otro dispositivo.
6. Desde otro PC o celular de la misma red, abrir:

```text
http://IP_DEL_HOST:8080/
```

## Archivos generados por build

Al compilar, el postprocesador de build genera archivos auxiliares junto al `.exe`:

- `README_SERVIDOR_FERIA.txt`: guia del servidor y del leaderboard web. Si solo se quiere probar el juego, se ignora.
- `REINICIAR_DATOS_JUEGO.bat`: acceso rapido para reiniciar datos locales del equipo.
- `REINICIAR_DATOS_JUEGO.ps1`: script PowerShell de reinicio local.

Estos scripts limpian la persistencia local del equipo donde se ejecutan y recrean `Application.persistentDataPath/db/` desde las semillas del build. No borran la base SQLite del servidor host ni datos de otros PCs.

## Prueba de humo técnica

Con el servidor corriendo:

```powershell
python .\smoke_test.py
```

La prueba valida que el servidor responde y que los endpoints principales no estan caidos. No convierte la sincronizacion completa de perfiles entre PCs en alcance final.

## Criterio de cierre

Consideramos correcto el add-on cuando:

- el servidor abre en el host;
- `/health` responde;
- la pagina `/` muestra el ranking;
- otros dispositivos pueden visualizar la pagina web del host;
- el host conserva su SQLite del evento;
- la documentación comunica que solo el leaderboard del host es el resultado confiable de feria.

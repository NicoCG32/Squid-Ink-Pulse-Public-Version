# Guia paso a paso para montar la feria

Esta guia describe el flujo completo para preparar una sesion de feria cuando se parte desde cero: ningun PC tiene build del juego, aun no hay servidor corriendo y los 3 o 4 equipos deben quedar conectados al mismo ranking.

El objetivo operativo es simple:

- un PC actua como host del servidor;
- cada PC de juego abre el `.exe`;
- cada jugador entra con `nickname` + `codigo`, o se crea como nuevo jugador;
- el progreso se guarda localmente durante la sesion y se envia al servidor al salir correctamente desde el menu principal;
- el ranking web se puede ver desde el host o desde cualquier PC de la misma red.

## 1. Roles de los PCs

Antes de empezar, asignar roles evita confusiones.

| Rol | Puede ser el mismo PC que otro rol | Responsabilidad |
| --- | --- | --- |
| PC de build | Si | Abre Unity y genera el build Windows. |
| PC host servidor | Si | Ejecuta `Tools/FairServer` y guarda la base SQLite del evento. |
| PCs cliente/juego | Si | Ejecutan el `.exe` y se conectan al servidor por LAN. |
| Pantalla ranking | Si | Muestra `http://IP_DEL_HOST:8080/` en navegador. |

Configuracion comun recomendada para una feria pequena:

```text
PC-01: host servidor + ranking + opcionalmente juego
PC-02: juego
PC-03: juego
PC-04: juego
```

## 2. Requisitos previos

En el PC que genera el build:

- repositorio actualizado;
- Unity Hub instalado;
- Unity `6000.3.11f1`, o la version exacta indicada por `ProjectSettings/ProjectVersion.txt`;
- proyecto abierto al menos una vez sin errores de importacion.

En el PC host del servidor:

- Windows;
- Python 3 instalado y disponible desde PowerShell como `python`, o desde `.bat` como `py -3`;
- carpeta `Tools/FairServer/` copiada desde el repositorio;
- permiso de firewall para recibir conexiones TCP por el puerto `8080`.

En cada PC cliente:

- Windows;
- la carpeta completa del build;
- conexion a la misma red local que el host;
- acceso al puerto `8080` del host.

Materiales practicos:

- pendrive o carpeta compartida para distribuir el build;
- una hoja fisica o planilla para anotar `nickname` y `codigo` de cada jugador nuevo;
- cable de red o Wi-Fi estable;
- un nombre de maquina por PC, por ejemplo `PC-01`, `PC-02`, `PC-03`, `PC-04`.

## 3. Checklist prebuild

Antes de crear el build, confirmar el estado del proyecto. Esta fase evita distribuir un ejecutable que abre, pero falla durante la feria.

1. Abrir el proyecto en Unity.
2. Esperar a que termine la importacion.
3. Revisar que la consola no tenga errores rojos.
4. Entrar al `MainMenu` en Play Mode.
5. Confirmar que los botones principales responden.
6. Confirmar que la tienda abre.
7. Confirmar que una partida puede iniciar en `ZonaEpipelagica`.
8. Confirmar que pausa, game over y regreso a menu no bloquean el flujo.
9. Salir de Play Mode.
10. Guardar escenas y assets si Unity muestra cambios pendientes.

Si esta fase falla, no generar build. Primero corregir el problema en Unity y repetir el checklist.

## 4. Generar el build desde Unity

Estos pasos se hacen una vez, en el PC de build.

1. Abrir Unity Hub.
2. Abrir el proyecto `Squid-Ink-Pulse`.
3. Esperar a que Unity termine de importar.
4. Revisar que no existan errores rojos en la consola.
5. Abrir `File > Build Profiles` o `File > Build Settings`, segun la interfaz de Unity.
6. Seleccionar plataforma Windows.
7. Confirmar que las escenas incluidas y activas sean:

```text
Assets/Scenes/MainMenu/MainMenu.unity
Assets/Scenes/Game/ZonaEpipelagica.unity
Assets/Scenes/Game/ZonaAbisopelagica.unity
Assets/Scenes/Game/ZonaTutorial.unity
Assets/Scenes/ShopMenu/ShopMenu.unity
```

8. Si Unity pide cambiar plataforma, aceptar `Switch Platform`.
9. Elegir una carpeta de salida clara, por ejemplo:

```text
Builds/Feria/SquidInkPulse_Feria_2026-07-01/
```

10. Ejecutar `Build`.
11. Esperar a que termine sin errores.

La carpeta final debe contener el `.exe` y sus archivos acompanantes. No se debe copiar solamente el `.exe`; Unity necesita tambien la carpeta `_Data` y las DLL generadas.

Ejemplo de carpeta valida:

```text
SquidInkPulse_Feria_2026-07-01/
|- Squid Ink Pulse.exe
|- Squid Ink Pulse_Data/
|- UnityPlayer.dll
|- README_CLIENTE_FERIA.txt
|- REINICIAR_DATOS_JUEGO.bat
|- REINICIAR_DATOS_JUEGO.ps1
`- otros archivos generados por Unity
```

`README_CLIENTE_FERIA.txt` se genera automaticamente al terminar el build. Incluye instrucciones de cliente con el marcador `<IP_DEL_HOST>` para configurar el acceso directo contra el servidor de feria.

`REINICIAR_DATOS_JUEGO.bat` y `REINICIAR_DATOS_JUEGO.ps1` tambien se generan automaticamente. Permiten limpiar el progreso local de ese PC cliente y recrear la base `db` desde las semillas incluidas en el build.

## 5. Probar el build en el PC de build

Antes de distribuir, hacer una prueba minima.

1. Abrir el `.exe`.
2. Confirmar que aparece la interfaz de feria con campos de:
   - nick;
   - codigo;
   - servidor;
   - nuevo jugador.
3. Cerrar el juego.

La interfaz de feria solo aparece si el servidor configurado responde a `/health`. Para probarla antes de distribuir, levantar primero el servidor local o usar un acceso directo con `--fair-server=http://IP_DEL_HOST:8080`.

Si no aparece la interfaz de feria, revisar:

- que el servidor este corriendo;
- que la URL configurada responda a `/health`;
- que el acceso directo o comando no este usando:

```text
--fair-disabled
```

Ese argumento desactiva el modo feria.

## 6. Preparar la carpeta del servidor en el host

En el PC host, crear una estructura simple:

```text
C:\SquidFeria\
|- Server\
`- Game\
```

Copiar el contenido de:

```text
Tools/FairServer/
```

hacia:

```text
C:\SquidFeria\Server\
```

La carpeta del servidor debe quedar asi:

```text
C:\SquidFeria\Server\
|- server.py
|- start_fair_server.ps1
|- start_fair_server.bat
|- smoke_test.py
`- README.md
```

La primera vez que se inicie, el servidor creara automaticamente:

```text
C:\SquidFeria\Server\data\fair_server.sqlite3
```

Ese archivo es la base de datos del evento. Contiene participantes, codigos, snapshots y ranking.

## 7. Copiar el build a los PCs

Copiar la carpeta completa del build a cada PC de juego.

Ruta sugerida en cada equipo:

```text
C:\SquidFeria\Game\
```

Debe quedar algo como:

```text
C:\SquidFeria\Game\
|- Squid Ink Pulse.exe
|- Squid Ink Pulse_Data\
|- UnityPlayer.dll
|- README_CLIENTE_FERIA.txt
|- REINICIAR_DATOS_JUEGO.bat
|- REINICIAR_DATOS_JUEGO.ps1
`- otros archivos generados por Unity
```

Regla importante:

```text
Si falta la carpeta *_Data, el juego no es un build completo.
```

El cliente debe leer `README_CLIENTE_FERIA.txt` y reemplazar `<IP_DEL_HOST>` por la IPv4 real del PC host antes de crear el acceso directo.

Si el cliente necesita empezar desde cero en ese PC, debe cerrar el juego y ejecutar `REINICIAR_DATOS_JUEGO.bat`. El script pide confirmacion escribiendo `SI`, respalda los datos actuales y restaura los JSON limpios desde `Squid Ink Pulse_Data/StreamingAssets/db/`.

## 8. Conectar todos los PCs a la misma red

Todos los equipos deben estar en la misma red local.

En el PC host:

1. Abrir PowerShell.
2. Ejecutar:

```powershell
ipconfig
```

3. Buscar el adaptador activo.
4. Anotar la direccion `IPv4`.

Ejemplo:

```text
Direccion IPv4 . . . . . . . . . . : 192.168.1.50
```

En este ejemplo, la URL del servidor para los clientes sera:

```text
http://192.168.1.50:8080
```

Desde un PC cliente se puede comprobar conectividad basica con:

```powershell
ping 192.168.1.50
```

Si el `ping` falla, puede ser por firewall o por estar en redes distintas. Lo decisivo para el juego es que el navegador del cliente pueda abrir `/health`, como se indica mas abajo.

## 9. Iniciar el servidor

En el PC host:

1. Abrir la carpeta:

```text
C:\SquidFeria\Server\
```

2. Hacer click derecho en un espacio vacio y abrir PowerShell en esa carpeta.
3. Ejecutar:

```powershell
.\start_fair_server.ps1
```

Alternativa:

```text
doble click en start_fair_server.bat
```

El servidor queda escuchando en:

```text
http://0.0.0.0:8080
```

Conceptualmente, `0.0.0.0` significa que el servidor acepta conexiones desde las interfaces de red del host. Para abrirlo desde navegador se usa:

```text
http://localhost:8080/
```

en el propio host, o:

```text
http://IP_DEL_HOST:8080/
```

desde otros PCs.

## 10. Verificar que el servidor responde

En el PC host, abrir navegador y entrar a:

```text
http://localhost:8080/health
```

Debe responder con texto JSON de estado.

Luego abrir:

```text
http://localhost:8080/
```

Debe aparecer la pagina web de ranking.

Desde cada PC cliente, abrir en navegador:

```text
http://IP_DEL_HOST:8080/health
```

Ejemplo:

```text
http://192.168.1.50:8080/health
```

Si funciona en el host pero no en los clientes, casi siempre el problema es firewall, IP incorrecta o equipos en redes distintas.

## 11. Permitir el servidor en Firewall de Windows

Cuando Windows pregunte si Python puede aceptar conexiones, permitirlo para red privada.

Si no aparece aviso, crear una regla manual.

Opcion por interfaz:

1. Abrir `Seguridad de Windows`.
2. Entrar a `Firewall y proteccion de red`.
3. Entrar a `Permitir una aplicacion a traves del firewall`.
4. Permitir Python en redes privadas.
5. Si hay varias entradas de Python, permitir la que corresponda a Python 3.

Opcion por PowerShell como administrador:

```powershell
New-NetFirewallRule -DisplayName "Squid Fair Server 8080" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow
```

Despues de crear la regla, repetir la prueba desde un PC cliente:

```text
http://IP_DEL_HOST:8080/health
```

## 12. Configurar el juego para conectarse al host

Hay dos formas practicas.

### Opcion A: configurar desde la interfaz del juego

En cada PC cliente:

1. Abrir el `.exe`.
2. En el campo `Servidor`, escribir:

```text
http://IP_DEL_HOST:8080
```

Ejemplo:

```text
http://192.168.1.50:8080
```

3. Crear jugador nuevo o recuperar jugador existente.
4. El juego guarda esa URL localmente para siguientes ejecuciones en ese PC.

Esta opcion es mas simple durante pruebas.

### Opcion B: configurar por acceso directo

En cada PC cliente:

1. Crear un acceso directo al `.exe`.
2. Abrir propiedades del acceso directo.
3. En `Destino`, agregar argumentos al final.

Ejemplo para `PC-02`:

```text
"C:\SquidFeria\Game\Squid Ink Pulse.exe" --fair-server=http://192.168.1.50:8080 --fair-machine=PC-02
```

Tambien se acepta este formato:

```text
"C:\SquidFeria\Game\Squid Ink Pulse.exe" --fair-server http://192.168.1.50:8080 --fair-machine PC-02
```

El argumento debe apuntar a la base del servidor, no a `/health`. Correcto:

```text
--fair-server=http://192.168.1.50:8080
```

Incorrecto:

```text
--fair-server=http://192.168.1.50:8080/health
```

Ejemplo para `PC-03`:

```text
"C:\SquidFeria\Game\Squid Ink Pulse.exe" --fair-server=http://192.168.1.50:8080 --fair-machine=PC-03
```

Ejemplo para `PC-04`:

```text
"C:\SquidFeria\Game\Squid Ink Pulse.exe" --fair-server=http://192.168.1.50:8080 --fair-machine=PC-04
```

El argumento `--fair-machine` ayuda al servidor a distinguir sesiones por equipo.

Para pruebas fuera de feria se puede desactivar el overlay con:

```text
--fair-disabled
```

No usar ese argumento durante la feria.

## 13. Crear un jugador nuevo

En el juego:

1. Escribir un `nickname`.
2. Confirmar que el campo `Servidor` apunte al host correcto.
3. Presionar `Nuevo jugador`.
4. El servidor crea el participante.
5. El juego muestra o recibe un `codigo` de recuperacion.
6. Anotar inmediatamente:

```text
nickname + codigo
```

Ejemplo de registro manual:

```text
NICO - 4821
VALE - 9130
MARTIN - 2754
```

Ese codigo permite recuperar el perfil desde otro PC o en otro momento del evento.

## 14. Recuperar un jugador existente

En el juego:

1. Escribir el mismo `nickname`.
2. Escribir su `codigo`.
3. Confirmar que el servidor sea correcto.
4. Presionar `Entrar`.

Si el jugador estaba activo en otro PC, el servidor puede responder que la sesion ya esta activa.

Esto es intencional: el MVP usa sesion exclusiva para evitar que dos PCs modifiquen el mismo perfil al mismo tiempo.

## 15. Como se guarda el progreso durante la feria

El flujo actual es:

1. Al crear o recuperar jugador, Unity recibe el snapshot remoto.
2. Unity aplica ese snapshot al perfil local del PC.
3. El jugador juega normalmente.
4. Mientras la sesion esta abierta, Unity mantiene un `heartbeat` para que el servidor sepa que el participante sigue activo.
5. Al salir desde el menu principal, Unity crea un snapshot final.
6. Unity envia ese snapshot final al servidor mediante `checkout`.
7. El servidor libera la sesion.

Punto critico:

```text
Para cerrar bien una sesion de feria, el jugador debe salir desde el menu principal.
```

Si se cierra el juego con Alt+F4, apagado del PC o cierre forzado, el servidor liberara la sesion despues del timeout, pero el ultimo snapshot podria no haberse enviado.

## 16. Mostrar el ranking

En el PC host:

```text
http://localhost:8080/
```

En otro PC o pantalla de la misma red:

```text
http://IP_DEL_HOST:8080/
```

Ejemplo:

```text
http://192.168.1.50:8080/
```

El ranking usa los datos guardados por el servidor en SQLite.

## 17. Prueba completa antes de abrir la feria

Hacer esta prueba antes de recibir jugadores.

1. Iniciar servidor en el host.
2. Abrir `http://localhost:8080/health`.
3. Abrir `http://localhost:8080/`.
4. En `PC-02`, abrir el juego.
5. Apuntar el servidor a `http://IP_DEL_HOST:8080`.
6. Crear jugador `TEST01`.
7. Anotar el codigo.
8. Entrar al juego y jugar unos segundos.
9. Volver al menu principal.
10. Presionar salir para hacer checkout.
11. Confirmar que el ranking web muestra datos.
12. Reabrir el juego en `PC-02`.
13. Recuperar `TEST01` con su codigo.
14. Confirmar que el perfil carga.
15. Abrir el juego en `PC-03` e intentar recuperar el mismo `TEST01` mientras sigue activo en `PC-02`.
16. Confirmar que el servidor bloquea la doble sesion.
17. Salir correctamente desde `PC-02`.
18. Recuperar `TEST01` desde `PC-03`.
19. Confirmar que ahora permite entrar.
20. Crear jugadores distintos en `PC-02`, `PC-03` y `PC-04`.
21. Verificar que los tres pueden jugar al mismo tiempo.

Si esta prueba pasa, el montaje esta listo para operacion basica.

## 18. Resetear datos antes de la feria real

Si se hicieron pruebas y se quiere empezar limpio:

1. Cerrar el servidor.
2. Ir a:

```text
C:\SquidFeria\Server\data\
```

3. Respaldar o borrar:

```text
fair_server.sqlite3
```

Opcion segura:

```text
fair_server_prueba_2026-07-01.sqlite3
```

4. Volver a iniciar el servidor.

Al iniciar sin base existente, el servidor crea una base nueva.

Advertencia:

```text
Borrar fair_server.sqlite3 elimina participantes, codigos, snapshots y ranking del evento.
```

Esto reinicia el servidor de feria. Para reiniciar solo un PC cliente, sin tocar el servidor ni otros jugadores, usar `REINICIAR_DATOS_JUEGO.bat` dentro de la carpeta del build en ese PC.

## 19. Operacion durante la feria

Mantener estas reglas:

- no cerrar la ventana del servidor mientras haya jugadores;
- mantener visible el ranking desde `http://localhost:8080/` o `http://IP_DEL_HOST:8080/`;
- anotar cada `nickname + codigo`;
- pedir que los jugadores salgan desde el menu principal;
- si un PC se cae, esperar el timeout de sesion antes de recuperar ese jugador en otro PC;
- no borrar la base SQLite durante el evento;
- hacer respaldo de la base al terminar.

Respaldo final recomendado:

```text
C:\SquidFeria\Server\data\fair_server.sqlite3
```

copiado como:

```text
fair_server_final_2026-07-01.sqlite3
```

## 20. Problemas frecuentes

### El juego no muestra la interfaz de feria

La interfaz solo se muestra cuando el servidor configurado responde a `/health`.

Revisar:

1. El servidor esta abierto.
2. La URL configurada es correcta.
3. `http://IP_DEL_HOST:8080/health` responde desde ese PC.
4. El juego no se abrio con:

```text
--fair-disabled
```

Tambien confirmar que se esta usando el build actualizado.

### El cliente no conecta al servidor

Revisar en este orden:

1. El servidor esta abierto en el host.
2. `http://localhost:8080/health` funciona en el host.
3. La IP usada por el cliente es la IPv4 correcta del host.
4. El cliente puede abrir `http://IP_DEL_HOST:8080/health`.
5. El firewall permite conexiones entrantes por puerto `8080`.
6. Host y cliente estan en la misma red.

### El servidor responde en el host, pero no en otros PCs

Causa probable:

```text
Firewall de Windows o red distinta.
```

Solucion:

- permitir Python en red privada;
- crear regla TCP `8080`;
- confirmar que todos esten en la misma LAN.

### Un jugador queda bloqueado por sesion activa

Esto ocurre cuando el mismo participante sigue activo en otro PC.

Soluciones:

- salir correctamente desde el PC donde estaba abierto;
- esperar el timeout si el PC se cerro mal;
- no crear dos sesiones simultaneas con el mismo `nickname + codigo`.

### El ranking no muestra lo ultimo jugado

El MVP envia el snapshot final al salir desde el menu principal.

Revisar:

- que el jugador haya salido desde el menu principal;
- que el servidor no se haya cerrado antes del checkout;
- que el cliente apunte al host correcto.

### Se cerro el servidor accidentalmente

Volver a iniciar:

```powershell
.\start_fair_server.ps1
```

Si la base `fair_server.sqlite3` sigue en `data/`, los datos persisten.

### Python no se reconoce

Instalar Python 3 o usar el lanzador:

```text
start_fair_server.bat
```

Si sigue fallando, reinstalar Python marcando la opcion de agregarlo al PATH.

### PowerShell no deja ejecutar el `.ps1`

Usar el `.bat`:

```text
start_fair_server.bat
```

Tambien se puede abrir PowerShell como administrador y permitir scripts para esa sesion:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Luego volver a ejecutar:

```powershell
.\start_fair_server.ps1
```

## 21. Checklist rapido de evento

Antes del evento:

- checklist prebuild completado;
- build Windows generado;
- build probado localmente;
- carpeta completa del build copiada a cada PC;
- servidor copiado al host;
- Python 3 funcionando en host;
- servidor iniciado;
- `/health` funciona en host;
- `/health` funciona desde cada cliente;
- firewall configurado;
- accesos directos apuntan a `http://IP_DEL_HOST:8080`;
- ranking visible;
- prueba con jugador `TEST01` completada;
- base de prueba respaldada o borrada antes de abrir feria real.

Durante el evento:

- servidor siempre abierto;
- codigos anotados;
- jugadores salen desde menu principal;
- no borrar `fair_server.sqlite3`.

Despues del evento:

- cerrar juego en clientes;
- cerrar servidor;
- respaldar `fair_server.sqlite3`;
- conservar la lista de `nickname + codigo` si se reutilizara el evento.

# Guía para probar el add-on de feria

## Alcance

Esta guía describe como probar el add-on de feria que desarrollamos como complemento de presentación. El juego principal no depende de esta configuración: se puede ejecutar, probar y compilar como juego local sin servidor.

El alcance final de feria es:

- un PC host ejecuta `Tools/FairServer/`;
- el host guarda una base SQLite local;
- el host muestra un leaderboard web;
- los resultados que se guardan formalmente son los jugados desde el PC host;
- otros dispositivos de la misma red solo pueden visualizar el leaderboard web desde un navegador.

No logramos cerrar como entrega final la sincronizacion completa de progreso, compras, skins, mejoras o recuperacion integral de jugadores entre PCs. Cada PC conserva su persistencia local del juego.

## Cuando ignorar esta guía

Si solo queremos probar el juego, esta guía se ignora. Basta con ejecutar el proyecto desde `MainMenu` o abrir un build local regenerado desde Unity.

La feria es secundaria frente a la entrega principal. Su documentación existe para explicar el servidor local y el leaderboard web, no para condicionar la revision del juego.

## Warnings rojos durante Unity o build

Durante la compilación, la importación o la primera apertura del build pueden aparecer warnings rojos por falta de host de feria, `localhost:8080` o conexión rechazada. Si estamos probando el juego normal, esos warnings se ignoran.

Solo se investigan cuando la prueba tiene por objetivo revisar feria. En ese caso, primero se debe levantar el servidor en el host.

También puede aparecer una excepción local de `DirectoryNotFoundException` bajo `Library/PackageCache` cuando el repositorio vive en una ruta demasiado larga. Esa carpeta está ignorada por Git y no forma parte del entregable; si el build termina en `Build Finished, Result: Success`, la mitigación práctica es abrir el proyecto desde una ruta más corta, por ejemplo `C:\Squid-Ink-Pulse`.

## Configuración recomendada

```text
PC-01: host del servidor + juego de feria + ranking web
PC-02: visualizacion del leaderboard en navegador
PC-03: visualizacion del leaderboard en navegador
```

Todos los equipos deben estar en la misma red local. El puerto usado por defecto es `8080`.

## Build local para prueba directa

La carpeta `Build/` no se versiona en Git porque es un artefacto generado por Unity y puede superar los límites de tamaño del repositorio. Para evitar la espera de importación, carga de datos y compilación en Unity, se mantiene un `.zip` con el juego compilado en Google Drive:

[Descargar build compilado de Squid Ink-Pulse](https://drive.google.com/drive/folders/18DUVTfJf5QDkqeJ5wqoowFUHdfWjI3YU?usp=sharing)

Después de descargarlo, descomprimir la carpeta completa y ejecutar `Squid Ink-Pulse.exe` desde esa misma carpeta.

Si ya existe un build local regenerado, se puede probar el juego normal ejecutando:

```text
Build/Squid Ink-Pulse.exe
```

La carpeta del build debe conservarse completa. El `.exe` depende de `Squid Ink-Pulse_Data/`, `UnityPlayer.dll` y los demás archivos generados por Unity.

## Compilar el juego solo si se requiere

Estos pasos son opcionales. Se usan si queremos regenerar el ejecutable, comprobar el pipeline de build o producir una carpeta nueva para probar el add-on de feria.

1. Abrir el proyecto en Unity.
2. Ir a `File > Build Profiles` o `File > Build Settings`.
3. Seleccionar Windows.
4. Confirmar que las escenas de entrega estén habilitadas y en orden.
5. Elegir una carpeta de salida fuera de `Assets/`.
6. Ejecutar `Build` o `Build And Run`.

El build debe distribuirse como carpeta completa. No basta con copiar el `.exe`; también deben mantenerse `UnityPlayer.dll`, la carpeta `*_Data` y los demás archivos generados.

Cada build genera ademas:

```text
README_SERVIDOR_FERIA.txt
REINICIAR_DATOS_JUEGO.bat
REINICIAR_DATOS_JUEGO.ps1
```

`README_SERVIDOR_FERIA.txt` es una guía del servidor de feria. Si solo se quiere probar el juego, se ignora.

## Atajo para probar tienda

Para testear compras sin farmear camarones, desde `MainMenu` o `ShopMenu` se puede escribir directamente con el teclado:

```text
SONICYNOTA7
```

No existe campo de entrada visible. Cada vez que se completa el código, el juego acredita `676700` camarones de prueba. El código se puede repetir para probar mejoras, skins, estados de compra y paginación de tienda.

## Levantar el servidor en el host

En el PC host:

1. Abrir `Tools/FairServer/`.
2. Ejecutar:

```powershell
.\start_fair_server.ps1
```

O usar doble click en:

```text
start_fair_server.bat
```

3. Verificar:

```text
http://localhost:8080/health
```

4. Abrir el leaderboard:

```text
http://localhost:8080/
```

5. Ejecutar el juego en este mismo PC host si se quieren guardar resultados en la base del evento.

La base del evento queda en:

```text
Tools/FairServer/data/fair_server.sqlite3
```

Solo esa base del host almacena el leaderboard compartido. Si se quiere conservar evidencia del evento, se debe respaldar ese archivo.

## Ver el leaderboard desde otros dispositivos

Desde el PC host, obtener la IPv4:

```powershell
ipconfig
```

Desde otro PC o celular conectado a la misma red, abrir en navegador:

```text
http://IP_DEL_HOST:8080/
```

Esto solo muestra el leaderboard web del host. No habilita guardado remoto desde el dispositivo externo.

## Que debe observarse

Al probar feria:

- el servidor responde en el PC host;
- el ranking web del host queda disponible;
- el host conserva la base SQLite del evento;
- otros dispositivos pueden visualizar `http://IP_DEL_HOST:8080/`;
- los resultados guardados son los del PC host.

Si se abre el juego sin servidor y el objetivo es probar el juego normal, los warnings de host se consideran esperados.

## Reiniciar datos locales

Dentro de la carpeta del build se generan scripts para reiniciar la persistencia local del equipo donde se ejecuta el juego:

```text
REINICIAR_DATOS_JUEGO.bat
REINICIAR_DATOS_JUEGO.ps1
```

El reinicio local limpia:

- camarones;
- mejoras permanentes;
- skins compradas;
- mejor puntaje;
- runs;
- leaderboard local del PC.

No borra:

- la base SQLite del servidor host;
- datos de otros PCs;
- `PlayerPrefs`, salvo que se ejecute el `.ps1` con `-IncludePlayerPrefs`.

Dentro del repositorio, el script equivalente para limpiar la persistencia local del equipo de desarrollo es:

```text
Tools/CleanPersistentData.bat
```

Tambien se puede ejecutar:

```powershell
.\Tools\CleanPersistentData.ps1
```

## Respaldar o limpiar la feria

Para respaldar el evento, copiar:

```text
Tools/FairServer/data/fair_server.sqlite3
```

Para reiniciar el leaderboard de feria del host, cerrar el servidor y borrar o renombrar ese archivo. Esta acción afecta solo al host.

## Problemas frecuentes

### El juego funciona, pero aparecen warnings rojos

Si no estamos probando feria, se ignoran. Son esperados cuando no hay host activo.

### Otro dispositivo no ve el leaderboard

Revisar:

- IP correcta del host;
- que todos los dispositivos esten en la misma red;
- firewall de Windows;
- que el servidor siga abierto;
- que el puerto usado sea `8080`.

### Se esperaba guardar resultados desde otro PC

Esa funcionalidad no quedo cerrada como alcance final. Documentamos la feria como add-on de leaderboard host; la progresión completa del jugador sigue siendo local por dispositivo.

## Resumen operativo

- Build Windows generado o build adjunto disponible.
- `README_SERVIDOR_FERIA.txt` presente junto al `.exe` si se recompilo.
- Scripts de reinicio local presentes junto al `.exe`.
- Servidor abierto en el PC host.
- `/health` responde en el host.
- `/` muestra leaderboard en el host.
- Otros dispositivos pueden visualizar el leaderboard web.
- Se entiende que solo el leaderboard del host es el resultado compartido confiable.

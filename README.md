# Squid Ink-Pulse

Squid Ink-Pulse es un videojuego 2D desarrollado en Unity por nuestro equipo Yeco Works. Como equipo desarrollamos un endless runner de acción y riesgo, ambientado en un ecosistema submarino hostil, donde el jugador controla a un calamar bebé que avanza de forma continua, evita amenazas, recolecta camarones y domina la habilidad Ink-Pulse.

La propuesta jugable que construimos no se limita a esquivar obstáculos: buscamos que el riesgo sea una herramienta. Al pasar cerca de enemigos o trampas sin colisionar, el jugador carga Ink-Pulse; al activarlo, obtiene una ventana breve de ventaja para sostener el ritmo, atravesar situaciones críticas y extender la run.

## Alcance final del proyecto

La entrega principal funciona como juego local y no requiere servicios externos para jugarse.

Como equipo implementamos:

- Movimiento continuo del jugador con límites de escena y progresión de velocidad.
- Mecánica de graze para cargar Ink-Pulse mediante proximidad al peligro.
- Tres enemigos comunes implementados:
	- `Pez Globo`: se expande si el jugador se acerca y realiza un movimiento vertical leve.
	- `Mina`: obstáculo fijo y estático.
	- `Caña de Pescar`: aparece frente al jugador para evitar patrones estáticos y cuenta con animación de caída.
- Dos bosses implementados:
	- `SS Carnage`: barco pesquero jefe de la zona `ZonaEpipelagica`, con ataque final de red gigante que es inevitable si no se usa Ink-Pulse.
	- `Eel`: anguila eléctrica jefe de la zona `ZonaAbisopelagica`; lanza rayos evitables con dinámica tipo flappy bird, pero cierra con un rayo final inevitable si no se usa Ink-Pulse.
- Spawn progresivo de enemigos y obstáculos, con flujo de intensidad creciente durante la run.
- Zonas jugables `ZonaEpipelagica` y `ZonaAbisopelagica`, conectadas por portales.
- Comic tutorial simple accesible desde el botón `Cómo Jugar` en `MainMenu`.
- Tienda temporal dentro de la run mediante `DealerFish`.
- Gadgets de run: `Shell Shield` e `Ink-Bottle`.
- Tienda permanente en `ShopMenu` para skins y mejoras persistentes.
- Perfil local, records, economía de camarones, desbloqueables y ranking local.
- Comics de lore para introducción, portales, tienda y derrota.
- HUD, pausa, opciones, menú principal y pantalla de Game Over.
- Soundtrack dinámico con mezcla durante Ink-Pulse y progresión de pitch.
- Volumen maestro global que afecta soundtrack normal, soundtrack Ink-Pulse, botones, inkbar y SFX.

## Próxima prioridad: port móvil

La siguiente meta de producto es portar Squid Ink-Pulse a teléfonos, aprovechando que el proyecto ya está desarrollado en Unity. El objetivo no es rehacer el juego, sino adaptar la base actual para que sea jugable en dispositivos móviles.

Alcance inicial esperado:

- Controles táctiles para movimiento, Ink-Pulse, pausa, tiendas y menús.
- UI legible y operable en pantallas pequeñas y distintas relaciones de aspecto.
- Revisión de rendimiento, memoria, tiempos de carga y consumo de batería.
- Build Android como primera plataforma móvil; iOS queda sujeto a una iniciativa posterior y a disponibilidad de entorno de compilación.
- Mantener la experiencia principal: progresión, zonas, enemigos, bosses, tiendas, skins, comics, audio y persistencia local.

Este port debe tratarse como actualización mayor sobre el producto base cerrado, no como requisito pendiente para que el juego exista.

El alcance, las decisiones de plataforma y la matriz de validación se definen en [Docs/MobilePort.md](Docs/MobilePort.md).

## Add-on de feria

También implementamos un soporte de feria como extensión operativa, no como requisito para jugar. Este add-on permite levantar un servidor local en un PC host y mostrar un leaderboard web en la red local.

Alcance real del add-on:

- El PC host ejecuta `Tools/FairServer/`.
- El host guarda una base SQLite local.
- El resultado confiable de la feria es el leaderboard alojado en el host.
- Los resultados que se guardan formalmente son los jugados desde el PC host.
- Los dispositivos ajenos al host solo visualizan el leaderboard web desde un navegador.
- El juego local de cada PC sigue usando su propia persistencia.
- La sincronización completa de progreso, compras, skins o recuperación integral entre PCs no quedó cerrada como funcionalidad final de entrega.

Por esta razón, si se abre el juego o se genera un build sin servidor de feria activo, Unity puede mostrar warnings rojos relacionados con la falta de host o con `localhost:8080`. Esos mensajes se pueden ignorar cuando se está probando el juego local normal. Solo importan si se quiere probar explícitamente el add-on de feria.

## Requisitos técnicos

- Unity `6000.3.11f1`, según `ProjectSettings/ProjectVersion.txt`.
- Plataforma objetivo recomendada: Windows.
- Dependencias administradas por Unity Package Manager desde `Packages/manifest.json`.
- Universal Render Pipeline, Input System, TextMesh Pro, UGUI y paquetes 2D de Unity.

## Estructura del repositorio

- `Assets/Implementation/Code/`: código C# organizado por dominios de juego.
- `Assets/Implementation/Config/`: configuración técnica y perfiles reutilizables.
- `Assets/Content/`: prefabs, arte, audio, animaciones y contenido runtime.
- `Assets/Scenes/`: escenas principales del juego.
- `Assets/StreamingAssets/db/`: semillas JSON para perfil, records, catálogo y ranking local.
- `Docs/`: documentación técnica, arquitectura, sistemas y feria.
- `Tools/FairServer/`: servidor opcional para leaderboard de feria.
- `Packages/`: manifiesto y bloqueo de dependencias Unity.
- `ProjectSettings/`: configuración del proyecto Unity.

## Escenas incluidas en build

El build debe conservar las escenas habilitadas en este orden:

```text
Assets/Scenes/MainMenu/MainMenu.unity
Assets/Scenes/Game/ZonaEpipelagica.unity
Assets/Scenes/Game/ZonaAbisopelagica.unity
Assets/Scenes/ShopMenu/ShopMenu.unity
```

`MainMenu` es la escena de entrada. Desde ella se accede al juego, al comic tutorial de `Cómo Jugar`, a la tienda permanente, a opciones y a salida.

## Ejecución local en Unity

1. Abrir Unity Hub.
2. Seleccionar `Add project from disk`.
3. Elegir la carpeta raíz del repositorio.
4. Abrir el proyecto con Unity `6000.3.11f1`.
5. Esperar la importación inicial de assets y paquetes.
6. Abrir `Assets/Scenes/MainMenu/MainMenu.unity`.
7. Presionar `Play`.

Si no está activo el servidor de feria, pueden aparecer warnings rojos por falta de host. Para pruebas locales del juego se ignoran, siempre que no existan errores de compilación C# ni referencias rotas de escena.

## Build local

La carpeta `Build/` no se versiona en Git. Es un artefacto generado por Unity, puede superar fácilmente el límite de tamaño de GitHub y se puede reconstruir desde el proyecto fuente.

Para evitar la espera de importación, carga de datos y compilación en Unity, también se mantiene un `.zip` con el juego compilado en Google Drive:

[Descargar build compilado de Squid Ink-Pulse](https://drive.google.com/drive/folders/18DUVTfJf5QDkqeJ5wqoowFUHdfWjI3YU?usp=sharing)

Después de descargarlo, descomprimir la carpeta completa y ejecutar `Squid Ink-Pulse.exe` desde esa misma carpeta.

Si ya existe un build local, se puede probar abriendo:

```text
Build/Squid Ink-Pulse.exe
```

El ejecutable debe mantenerse dentro de su carpeta de build junto a `Squid Ink-Pulse_Data/`, `UnityPlayer.dll` y los demás archivos generados. Si se mueve solo el `.exe`, Unity no encontrará sus datos runtime.

## Regenerar el build

Estas instrucciones sirven para reconstruir el ejecutable, probar el sistema de build o revisar el add-on de feria desde una compilación nueva.

1. Abrir `File > Build Profiles` o `File > Build Settings`.
2. Seleccionar plataforma Windows.
3. Ejecutar `Switch Platform` si Unity lo solicita.
4. Confirmar que las escenas listadas en este README estén habilitadas y en el orden indicado.
5. Elegir una carpeta de salida fuera de `Assets/`, por ejemplo:

```text
Builds/SquidInkPulse/
```

6. Presionar `Build` o `Build And Run`.

`Build/` y `Builds/` están ignoradas por Git. Si se necesita entregar una versión compilada, distribuir la carpeta completa del build por fuera del repositorio, por ejemplo mediante un archivo `.zip`, un release o un medio compartido externo. Para esta entrega, el `.zip` compilado está disponible en el enlace de Google Drive indicado en la sección `Build local`.

Durante la compilación o la primera apertura del build pueden aparecer warnings rojos asociados a la ausencia de servidor/host de feria. Si el objetivo es probar el juego normal, esos warnings se ignoran. Si el objetivo es probar feria, primero se debe levantar `Tools/FairServer/` en el PC host.

El resultado debe distribuirse como carpeta completa de build. No basta con copiar el `.exe`: también deben mantenerse la carpeta `*_Data`, `UnityPlayer.dll` y los demás archivos generados por Unity.

Cada build genera además:

- `README_SERVIDOR_FERIA.txt`: guía del servidor de feria y del leaderboard web. Si solo se quiere probar el juego, se ignora.
- `REINICIAR_DATOS_JUEGO.bat`.
- `REINICIAR_DATOS_JUEGO.ps1`.

## Atajos de testeo

Para facilitar pruebas de la tienda permanente, desde `MainMenu` o `ShopMenu` se puede escribir el código secreto:

```text
SONICYNOTA7
```

No hay campo de texto visible: se escribe directamente con el teclado mientras se está en el menú. Cada ingreso completo acredita `676700` camarones de prueba. El código puede repetirse varias veces y no consume ninguna recompensa; su objetivo es permitir probar compras, mejoras y skins sin tener que farmear recursos durante una run.

## Probar el add-on de feria

Para probar feria:

1. En el PC host, abrir `Tools/FairServer/`.
2. Ejecutar `start_fair_server.bat` o `start_fair_server.ps1`.
3. Verificar en el host: `http://localhost:8080/health`.
4. Abrir el leaderboard: `http://localhost:8080/`.
5. Ejecutar el juego en el mismo PC host para registrar resultados en la base local del evento.
6. Obtener la IP del host con `ipconfig` si se quiere mostrar el ranking desde otro dispositivo.
7. En otro PC o celular conectado a la misma red, abrir:

```text
http://IP_DEL_HOST:8080/
```

El resultado esperado es que el host mantenga y muestre el leaderboard del evento. Los dispositivos ajenos al host solo visualizan ese ranking; no guardan resultados remotos ni comparten progreso completo entre PCs.

## Persistencia local

El juego carga datos base desde `Assets/StreamingAssets/db/` y guarda el progreso real en `Application.persistentDataPath/db/`. La persistencia local incluye:

- Perfil del jugador.
- Camarones acumulados.
- Skins compradas y skin equipada.
- Niveles de mejoras permanentes.
- Records y mejor puntaje.
- Catálogo de desbloqueables.
- Ranking local.

Para restablecer una sesión limpia de pruebas, cerrar el juego y eliminar la carpeta `db` ubicada dentro de `Application.persistentDataPath`. Al iniciar nuevamente, el juego recrea los archivos desde las semillas incluidas en `Assets/StreamingAssets/db/`.

Desde el repositorio, el reinicio recomendado es:

```text
Tools/CleanPersistentData.bat
```

También puede ejecutarse con PowerShell:

```powershell
.\Tools\CleanPersistentData.ps1
```

En builds generados por Unity, el equivalente queda junto al `.exe` como `REINICIAR_DATOS_JUEGO.bat` y `REINICIAR_DATOS_JUEGO.ps1`.

## Documentación principal

- [Índice de documentación](Docs/README.md)
- [Resumen del proyecto](Docs/ProjectOverview.md)
- [Arquitectura de software](Docs/SoftwareArchitecture.md)
- [Estructura del proyecto](Docs/ProjectStructure.md)
- [Sistemas núcleo](Docs/CoreSystems.md)
- [Sistemas de gameplay](Docs/GameplaySystems.md)
- [Enemigos y bosses](Docs/EnemiesAndBosses.md)
- [Portales](Docs/Portals.md)
- [Comics de lore](Docs/LoreComics.md)
- [UI y menú](Docs/UiAndMenu.md)
- [Servidor de feria](Docs/FairServer.md)
- [Guía de feria](Docs/FairEventSetupGuide.md)
- [Cierre de entrega](Docs/ROADMAP.md)
- [Informe de proyecto](Docs/ProjectReport.md)

## Equipo de desarrollo

El desarrollo del proyecto está a cargo de Yeco Works, equipo chileno de desarrollo de software organizado bajo metodología SCRUM.

- Pablo Guzmán (ICCI): SCRUM Master.
- Rodrigo Cortés (ICCI): Product Owner.
- Mauricio Muñoz (ICCI): Gameplay Programmer.
- Matías Palacios (ITI): Diseñador visual y sonoro.
- Inti Santibáñez (ICCI): Soporte de entrega.

Siglas:

- ICCI: Ingeniería Civil en Computación e Informática.
- ITI: Ingeniería en Tecnologías de la Información.

_Squid Ink-Pulse, Chile 2026_

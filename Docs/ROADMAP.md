# Cierre de entrega

## Proposito

Este documento formaliza el cierre de Squid Ink-Pulse como entrega final. Lo escribimos como equipo para separar con claridad lo que desarrollamos y entregamos, lo que queda documentado como add-on secundario de feria y lo que queda fuera del cierre actual.

## Alcance principal entregado

La entrega principal integra un endless runner 2D con progresión de run, economía persistente, tienda permanente, tienda temporal, skins, gadgets, comics narrativos, portales entre zonas, boss de zona epipelágica, boss abisal, comic de `Cómo Jugar` y menús globales.

Componentes incluidos:

- `ZonaEpipelagica` y `ZonaAbisopelagica` como zonas jugables conectadas por portales.
- Comic tutorial de `Cómo Jugar` accesible desde `MainMenu`.
- `BabySquid.prefab` como jugador canónico.
- Ink-Pulse con carga por graze, consumo visual progresivo y HUD `InkBar`.
- Tienda out-of-game `ShopMenu` para mejoras permanentes y skins.
- Tienda in-game por `DealerFish` para gadgets de run.
- Persistencia local por JSON bajo `Application.persistentDataPath/db/`.
- Comics de lore para inicio, portales, tienda y derrota.
- Soundtrack dinámico, crossfade Ink-Pulse y progresión de pitch.
- Volumen maestro global que afecta musica normal, musica Ink-Pulse, botones, inkbar y SFX.

## Add-on de feria entregado

También desarrollamos un add-on de feria para apoyar presentaciones presenciales. Este componente es opcional: el juego no depende de él para funcionar.

Alcance logrado:

- Servidor local en `Tools/FairServer/`.
- Base SQLite en el PC host.
- Pantalla web de leaderboard en `http://localhost:8080/`.
- Visualizacion del leaderboard desde otros dispositivos mediante `http://IP_DEL_HOST:8080/`.
- README de servidor generado automaticamente al compilar.
- Scripts generados para reiniciar persistencia local del equipo donde se ejecuta el build.

Limitacion importante:

- El resultado confiable del add-on es almacenar y mostrar el leaderboard del PC host.
- Los resultados guardados formalmente son los jugados desde el PC host; otros dispositivos solo visualizan el leaderboard web.
- La sincronizacion completa de progreso, compras, skins, mejoras o recuperacion integral entre PCs no quedo cerrada como parte funcional de la entrega.
- Por tanto, la persistencia principal sigue siendo local por dispositivo.

## Warnings esperados

Cuando el servidor de feria no está activo, Unity o el build pueden mostrar warnings rojos relacionados con la falta de host, `localhost:8080` o la conexión del add-on. Nosotros los consideramos esperados y se pueden ignorar durante pruebas locales del juego.

Solo deben investigarse si la prueba tiene como objetivo revisar feria. Para probar feria, primero hay que levantar `Tools/FairServer/` en el host y comprobar el leaderboard web.

## Estado de cierre

La entrega queda cerrada con estos componentes integrados:

- El build Windows abre desde `MainMenu` sin requerir servicios externos.
- MainMenu, pausa, opciones, Game Over, comic de `Cómo Jugar`, comics narrativos, portales y ambas tiendas no bloquean el flujo.
- Las compras permanentes modifican perfil, saldo y efectos de gameplay/economía según catálogo.
- Las skins compradas se guardan, se equipan y se aplican visualmente al entrar a gameplay.
- Los gadgets comprados durante la run se consumen al ejecutar su efecto y se limpian en Game Over.
- `ZonaAbisopelagica` conserva iluminacion compuesta y boss abisal.
- Ray y Jellyfish permanecen implementados pero no habilitados por balance (`baseWeight: 0`).
- El slider de volumen afecta todos los sonidos del juego.
- La feria se presenta como add-on opcional de leaderboard, no como persistencia remota completa.

## Extensiones fuera del cierre actual

Las siguientes líneas no condicionan la entrega final. Quedan registradas solo como posibles extensiones posteriores:

1. Ampliacion completa de feria.
   - Definir si se quiere sincronizacion real de perfil entre PCs.
   - Separar leaderboard de progreso completo.
   - Agregar pruebas LAN automatizadas si se retoma el sistema.

2. Pruebas extendidas de build Windows.
   - Probar sesiones largas por zona.
   - Revisar crecimiento de jerarquía runtime y limpieza fuera de pantalla.
   - Confirmar que pausa, Game Over, comics y tiendas restauran `Time.timeScale`.

3. Pulido audiovisual.
   - Ajustar feedback de portales, boss abisal, tienda y `ZonaAbisopelagica`.
   - Revisar mezcla relativa de soundtrack, Ink-Pulse, botones, inkbar y SFX.

4. Balance.
   - Ajustar curva de spawn, recompensas de camarones y multiplicadores permanentes.
   - Considerar activación gradual de Ray y Jellyfish.

## Regla de mantenimiento documental

Todo cambio posterior debe actualizar el documento que corresponda al contrato modificado:

- gameplay y reglas de jugador: `GameplaySystems.md`;
- escena, prefabs y jerarquía: `RuntimeHierarchyAudit.md`;
- cámara, boundaries y limpieza: `WorldAndCamera.md`;
- datos persistentes y tienda permanente: `PersistentProfile.md`;
- feria LAN: `FairServer.md` y `FairEventSetupGuide.md`;

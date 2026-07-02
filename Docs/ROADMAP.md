# Alcance de entrega y continuidad

## Proposito

Este documento resume el estado de entrega de Squid Ink-Pulse y ordena las extensiones recomendadas para una etapa posterior. No funciona como bitacora de tareas internas: el contrato tecnico vigente esta distribuido en `StateMachines.md`, `RuntimeHierarchyAudit.md`, `QATester.md`, `GameplaySystems.md` y los documentos de cada sistema.

## Alcance entregado

La entrega integra un endless runner 2D con progresion de run, economia persistente, tienda permanente, tienda temporal, skins, gadgets, comics narrativos, portales entre zonas, boss de zona epipelagica, boss abisal y soporte operativo para una feria LAN.

Componentes principales incluidos:
- `ZonaEpipelagica` y `ZonaAbisopelagica` como zonas jugables conectadas por portales.
- `ZonaTutorial` como secuencia dirigida de onboarding y prueba integrada.
- `BabySquid.prefab` como jugador canonico, con `SkinMount` para aplicar skins visuales.
- Ink-Pulse con carga por graze, consumo visual progresivo y HUD `InkBar`.
- Tienda out-of-game `ShopMenu` para mejoras permanentes y skins.
- Tienda in-game por `DealerFish` para gadgets de run de un solo uso.
- Persistencia local por JSON bajo `Application.persistentDataPath/db/`.
- Servidor de feria `Tools/FairServer/` con SQLite, ranking web, snapshots, recuperacion y checkout.
- Guia operativa de feria en `FairEventSetupGuide.md`.

## Criterios de aceptacion de entrega

La entrega se considera cerrada cuando se verifican estos puntos:
- El build Windows abre en modo local sin requerir servidor de feria.
- Si existe servidor de feria disponible, el overlay de feria permite crear o recuperar participante.
- MainMenu, pausa, opciones, Game Over, tutorial, comics, portales y ambas tiendas no bloquean el flujo.
- Las compras permanentes modifican perfil, saldo y efectos de gameplay/economia segun catalogo.
- Las skins compradas se guardan, se equipan y se aplican visualmente al entrar a gameplay.
- Los gadgets comprados durante la run se consumen al ejecutar su efecto y se limpian en Game Over.
- `ZonaAbisopelagica` conserva iluminacion compuesta y boss abisal sin cargar SS Carnage.
- Ray y Jellyfish permanecen implementados pero no habilitados por balance (`baseWeight: 0`).
- La documentacion de `Docs/` describe contratos, operacion y validacion sin depender de notas internas.

## Continuidad recomendada

Las siguientes lineas de trabajo son extensiones naturales, no requisitos para considerar cerrada esta entrega:

1. Prueba LAN completa con varios PCs reales.
   - Confirmar host, firewall, recuperacion de participantes, checkout y ranking visible.
   - Registrar evidencia operativa usando `FairEventSetupGuide.md`.

2. Pulido visual de prefabs tecnicos.
   - Reemplazar Square base de Ray y Jellyfish por arte final.
   - Ajustar colliders y escala desde prefab, manteniendo tags/layers y scripts existentes.

3. Activacion gradual de enemigos preparados.
   - Subir `baseWeight` de Ray o Jellyfish solo despues de validar lectura visual, dificultad y rendimiento.
   - Documentar cualquier cambio de balance en `EnemiesAndBosses.md` y `QATester.md`.

4. Ampliacion narrativa.
   - Agregar hitos de puntaje a `LoreComicPresenter.entries` si se decide ampliar la narrativa.
   - Mantener la regla de no crear UI visual por codigo.

5. Refinamiento arquitectonico posterior.
   - Separar una vista/presenter adicional de `InGameShopManager` solo si el canvas crece.
   - Renombrar `MainMenu` a `MainMenuController` solo con migracion controlada de referencias serializadas.

## Fuera de alcance de la entrega

Quedan fuera del alcance final:
- multiplayer en tiempo real;
- sincronizacion continua de todas las runs entre clientes;
- balance definitivo de Ray y Jellyfish habilitados;
- nuevos bosses fuera de los ya conectados;
- generacion automatica de arte, layouts o animaciones por codigo.

## Regla de mantenimiento documental

Todo cambio posterior debe actualizar el documento que corresponda al contrato modificado:
- gameplay y reglas de jugador: `GameplaySystems.md`;
- escena, prefabs y jerarquia: `RuntimeHierarchyAudit.md`;
- camara, boundaries y limpieza: `WorldAndCamera.md`;
- datos persistentes y tienda permanente: `PersistentProfile.md`;
- feria LAN: `FairServer.md` y `FairEventSetupGuide.md`;
- validacion manual: `QATester.md`.

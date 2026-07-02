# Squid-Ink-Pulse

Squid Ink-Pulse es un endless runner de acción y riesgo. Controlas a un pequeño calamar que persigue a su madre en un ciclo constante donde el peligro no se evita: se domina.

La mecánica central gira en torno a la adrenalina: acercarte a amenazas como tiburones, aves y trampas carga adrenalina para activar el Ink Pulse, un impulso clave para escapar o avanzar en momentos críticos.

## Estructura del repositorio

- `Docs/`: documentación del proyecto y convenciones.
- `Assets/Implementation/`: código y configuración técnica.
- `Assets/Content/`: prefabs, audio y arte runtime.
- `Assets/Content/Animations/`: clips y controladores de animación para runtime.
- `Assets/Scenes/`: escenas del juego.
- `Packages/` y `ProjectSettings/`: base Unity del proyecto.

## Compilación con Unity

Versión requerida:

- Unity `6000.3.11f1`, según `ProjectSettings/ProjectVersion.txt`.

Pasos recomendados:

1. Abrir Unity Hub.
2. Seleccionar `Add project from disk` y elegir la carpeta raíz del repositorio.
3. Abrir el proyecto con Unity `6000.3.11f1`.
4. Esperar a que termine la importación inicial.
5. Revisar la consola de Unity y corregir cualquier error rojo antes de compilar.
6. Abrir `File > Build Profiles` o `File > Build Settings`, según la interfaz de Unity.
7. Seleccionar plataforma Windows y aceptar `Switch Platform` si Unity lo solicita.
8. Confirmar que las escenas habilitadas sean:

```text
Assets/Scenes/MainMenu/MainMenu.unity
Assets/Scenes/Game/ZonaEpipelagica.unity
Assets/Scenes/Game/ZonaAbisopelagica.unity
Assets/Scenes/Game/ZonaTutorial.unity
Assets/Scenes/ShopMenu/ShopMenu.unity
```

9. Elegir una carpeta de salida fuera de `Assets/`, por ejemplo:

```text
Builds/Feria/SquidInkPulse_Feria/
```

10. Presionar `Build And Run`.

El resultado debe incluir el `.exe`, la carpeta `*_Data`, `UnityPlayer.dll` y los demás archivos generados por Unity. No copiar solamente el `.exe`; el build necesita la carpeta completa.

En builds Windows, el proyecto también genera automáticamente archivos auxiliares junto al ejecutable:

- `README_CLIENTE_FERIA.txt`
- `REINICIAR_DATOS_JUEGO.bat`
- `REINICIAR_DATOS_JUEGO.ps1`

Para montar varios PCs en modo feria, usar la guía completa: [Docs/FairEventSetupGuide.md](Docs/FairEventSetupGuide.md).

## Documentación

- [Docs/README.md](Docs/README.md)
- [Docs/ProjectOverview.md](Docs/ProjectOverview.md)
- [Docs/ProjectStructure.md](Docs/ProjectStructure.md)
- [Docs/AssetFlow.md](Docs/AssetFlow.md)
- [Docs/AnimationStandards.md](Docs/AnimationStandards.md)
- [Docs/StateMachines.md](Docs/StateMachines.md)
- [Docs/GameplaySystems.md](Docs/GameplaySystems.md)
- [Docs/CoreSystems.md](Docs/CoreSystems.md)
- [Docs/EnemiesAndBosses.md](Docs/EnemiesAndBosses.md)
- [Docs/WorldAndCamera.md](Docs/WorldAndCamera.md)
- [Docs/UiAndMenu.md](Docs/UiAndMenu.md)
- [Docs/ROADMAP.md](Docs/ROADMAP.md)
- [Docs/ProjectReport.md](Docs/ProjectReport.md)
- [Docs/Reports/InformeSquidInkPulse.md](Docs/Reports/InformeSquidInkPulse.md)

## Notas de organización

- Soundtrack y SFX en `Assets/Content/Audio/`.
- Diseños runtime de personajes, enemigos y escenarios en `Assets/Content/Art/`.
- Animaciones runtime en `Assets/Content/Animations/`.

## Equipo de desarrollo

El desarrollo del proyecto está a cargo de Yeco Works, equipo de desarrollo de software de Chile basado en metodología SCRUM.

- Pablo Guzmán (ICCI): SCRUM Master
- Rodrigo Cortés (ICCI): Product Owner
- Mauricio Muñoz (ICCI): Gameplay Programmer
- Matías Palacios (ITI): Diseñador Visual y Sonoro
- Inti Santibáñez (ICCI): QA / Tester

### Siglas

- ICCI: Ingeniería Civil en Computación e Informática
- ITI: Ingeniería en Tecnologías de la Información

# Flujo de assets

## Runtime en Unity

- `Assets/Content/Audio/Soundtrack/`: musica final para el juego.
- `Assets/Content/Audio/SFX/`: efectos de sonido finales.
- `Assets/Content/Art/Characters/`: sprites/modelos de personajes.
- `Assets/Content/Art/Enemies/`: sprites/modelos de enemigos.
- `Assets/Content/Art/Environments/`: arte de escenarios.
- `Assets/Content/Art/UI/`: recursos visuales de interfaz.
- `Assets/Content/Animations/Characters/`: animaciones de personajes.
- `Assets/Content/Animations/Enemies/`: animaciones de enemigos.
- `Assets/Content/Animations/Environment/`: animaciones de entorno.
- `Assets/Content/Animations/UI/`: animaciones de interfaz.
- `Assets/Content/Prefabs/`: prefabs listos para runtime.

## Prefabs actuales

- `Prefabs/Enemies/`: `PezGlobo`, `Mina`, `CanaPescar`.
- `Prefabs/Bosses/SSCarnage/`: `SSCarnage`, `BossNetWall`.
- `Prefabs/Gadgets/`: `ShellShield`, `InkBottle`.
- `Prefabs/Shop/`: `DealerFish`.
- `Prefabs/Portals/`: `ScenePortal`.
- `Prefabs/Collectibles/`: camarones normales y x10.

## Regla para prefabs

- El prefab define identidad visual, collider propio, capa/tag esperado y script de comportamiento propio.
- El prefab no debe guardar referencias a objetos de escena como jugador, camara o boundaries.
- Si necesita jugador o camara, el manager o el script los resuelve en runtime.
- Si necesita limites, usa `BoundaryReferenceResolver`.
- Si es gadget comprable, usa `GadgetShopItem`; no debe actuar como pickup directo.

## Runtime en UI MainMenu

- `Assets/Content/Animations/UI/MainMenu/Character/`
- `Assets/Content/Animations/UI/MainMenu/Background/`
- `Assets/Content/Animations/UI/MainMenu/Buttons/`
- `Assets/Content/Art/UI/MainMenu/Character/`
- `Assets/Content/Art/UI/MainMenu/Background/`
- `Assets/Content/Art/UI/MainMenu/Buttons/`
- `Assets/Content/Audio/UI/MainMenu/Character/`
- `Assets/Content/Audio/UI/MainMenu/Background/`
- `Assets/Content/Audio/UI/MainMenu/Buttons/`
- `Assets/Implementation/Code/MainMenu/`

## Flujo recomendado

1. Ubicar arte, audio y animaciones en la carpeta funcional correspondiente.
2. Integrar animaciones en prefabs o elementos de UI segun dominio.
3. Mantener colliders de gameplay en objetos claros y documentados.
4. Vincular logica desde scripts del dominio correspondiente.
5. Validar operacion en escena runtime.
6. Confirmar que no quedaron referencias de escena serializadas dentro del prefab.

## Soundtrack dinamico

Las versiones normal e intensa de una misma musica deben exportarse con:

- mismo punto inicial;
- mismo tempo;
- misma duracion o loop perfectamente equivalente;
- misma afinacion.

En `ZonaEpipelagica`, el nodo `Soundtrack` mantiene dos `AudioSource`: normal e `INK`. `InkPulseMusicCrossfader` las inicia sincronizadas y cruza volumen segun `InkPulseState.Active`.

Regla de mezcla:
- Si la pista `INK` es una mezcla completa alternativa, usar crossfade lineal complementario.
- Si en el futuro se usan stems complementarios que no duplican el mismo contenido, puede probarse `useEqualPowerCrossfade`.

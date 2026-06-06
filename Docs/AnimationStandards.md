# Estandares de animacion

## Ubicacion de archivos

- `Assets/Content/Animations/Characters/`: clips y controladores de personajes.
- `Assets/Content/Animations/Enemies/`: clips y controladores de enemigos.
- `Assets/Content/Animations/Environment/`: animaciones de escenario y props.
- `Assets/Content/Animations/UI/`: animaciones de interfaz y feedback visual.
- `Assets/Content/Animations/UI/MainMenu/Character/`: animaciones de personaje del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Background/`: animaciones de fondo del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Buttons/`: animaciones de botones del menu principal.

## Tipos de assets esperados

- Animation Clip: `.anim`
- Animator Controller: `.controller`
- Animator Override Controller: `.overrideController` cuando exista variante.

## Convenciones de nombres

- Clip: `Entidad_Accion`.
- Controller: `Entidad_Controller`.
- Override: `Entidad_Variante_Override`.

## Integracion con prefabs

1. Seleccionar el prefab o elemento UI correspondiente.
2. Asignar el `Animator Controller` al componente `Animator`.
3. Verificar parametros y transiciones requeridas por la logica.
4. Validar comportamiento en escena runtime.

## Reglas de separacion

- Las animaciones deben vivir preferentemente en hijos visuales, no en el root que gobierna logica.
- No animar `PlayerBoundaries` ni `CameraBoundaries`.
- No usar animaciones para corregir colliders que deberian estar ajustados en prefab o escena.
- En `BossNetWall`, las capas visuales pueden animarse, pero la altura de gameplay sigue viniendo de `PlayerBoundaries`.
- En UI, los managers actualizan estado; las animaciones solo presentan transicion o feedback.

## Checklist antes de commit

- El clip y/o controller estan en la carpeta de dominio correcta.
- El nombre cumple convencion.
- Se incluye el `.meta` de todo asset nuevo.
- La integracion funciona en la escena runtime objetivo.
- No se suben archivos fuente externos al runtime del juego.
- La animacion no introduce dependencias de escena dentro de un prefab.

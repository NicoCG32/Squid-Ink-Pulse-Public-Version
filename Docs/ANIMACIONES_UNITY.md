# Guía de Animaciones en Unity

Este documento define el estándar oficial de animaciones runtime del proyecto.

## Ubicación de archivos

- `Assets/Content/Animations/Characters/`: Clips y controllers de personajes.
- `Assets/Content/Animations/Enemies/`: Clips y controllers de enemigos.
- `Assets/Content/Animations/Environment/`: Animaciones de escenario y props.
- `Assets/Content/Animations/UI/`: Animaciones de interfaz y feedback visual.
- `Assets/Content/Animations/UI/MainMenu/Character/`: Animaciones de personaje del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Background/`: Animaciones de fondo del menu principal.
- `Assets/Content/Animations/UI/MainMenu/Buttons/`: Animaciones de botones del menu principal.

## Tipos de assets esperados

- Animation Clip: `.anim`
- Animator Controller: `.controller`
- Animator Override Controller (opcional): `.overrideController`

## Convenciones de nombres

- Clip: `Entidad_Accion`.
- Controller: `Entidad_Controller`.
- Override: `Entidad_Variante_Override`.

## Integración con prefabs

1. Seleccionar el prefab o elemento UI correspondiente.
2. Asignar el `Animator Controller` al componente `Animator`.
3. Verificar parámetros y transiciones requeridas por la lógica.
4. Validar comportamiento en escena runtime.

## Checklist antes de commit

- El clip y/o controller están en la carpeta de dominio correcta.
- El nombre cumple convención.
- Se incluye el `.meta` de todo asset nuevo.
- La integración funciona en la escena runtime objetivo.
- No se suben archivos fuente externos al runtime del juego.

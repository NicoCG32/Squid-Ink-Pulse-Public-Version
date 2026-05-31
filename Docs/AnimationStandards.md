# Estándares de animación

## Ubicación de archivos

- `Assets/Content/Animations/Characters/`: clips y controladores de personajes.
- `Assets/Content/Animations/Enemies/`: clips y controladores de enemigos.
- `Assets/Content/Animations/Environment/`: animaciones de escenario y props.
- `Assets/Content/Animations/UI/`: animaciones de interfaz y feedback visual.
- `Assets/Content/Animations/UI/MainMenu/Character/`: animaciones de personaje del menú principal.
- `Assets/Content/Animations/UI/MainMenu/Background/`: animaciones de fondo del menú principal.
- `Assets/Content/Animations/UI/MainMenu/Buttons/`: animaciones de botones del menú principal.

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

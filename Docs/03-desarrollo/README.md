# 03 · Desarrollo y mantenimiento

[← Índice](../README.md)

Esta sección está pensada para quien va a tocar el proyecto después de leer su presentación. El trabajo comenzó como un desarrollo académico de equipo. Las iteraciones posteriores conservan ese origen y deben distinguirse de lo entregado durante el curso.

## Ruta de trabajo

1. [Estructura y criterios de contribución](estructura-y-contribucion.md): encuentra la escena, componente o configuración que realmente gobierna el cambio.
2. [Pruebas y validación](pruebas-y-validacion.md): comprueba un cambio en EditMode, PlayMode, composición y, cuando corresponda, dispositivo real.
3. [Auditoría documental](auditoria-documental.md): detecta información histórica, afirmaciones sin validar y duplicados.

## Convenciones útiles

- Mantener **una responsabilidad clara por controlador** y estados explícitos para las transiciones relevantes.
- No duplicar valores/configuración que ya tienen un catálogo, un perfil o un contrato de entrada.
- Evitar referencias de escena dentro del prefab fuente del jugador; asignarlas donde corresponda y usar fallbacks solo como respaldo.
- No trasladar datos personales de pruebas ni archivos locales de perfil al repositorio.
- Distinguir lo **implementado**, lo **comprobado** y lo **planificado**.
- Registrar cambios de build y plataforma con su versión, procedencia y límite de validación.

No establecemos artificialmente sprints, responsables futuros ni métricas que no podemos acreditar. Los roles y acuerdos históricos se consultan en los [informes académicos](../04-academica/README.md).

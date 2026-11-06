# Pruebas y herramientas de validación

La suite combina pruebas EditMode de reglas y contratos con pruebas PlayMode de interacción y transiciones. Los tests viven en `Assets/Tests/EditMode/` y `Assets/Tests/PlayMode/`; referencian `SquidInkPulse.Runtime` y quedan fuera de los builds normales.

## Ejecutar en Unity

1. Abrir `Window > General > Test Runner`.
2. Ejecutar EditMode y después PlayMode.
3. Revisar los fallos, pruebas omitidas e inconclusas.

## Ejecutar en batch

Cerrar cualquier instancia de Unity que tenga abierto el proyecto. Desde la raíz del repositorio, ejecutar cada proceso por separado:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode -nographics -projectPath "$PWD" `
  -runTests -testPlatform EditMode `
  -testResults "$PWD\TestResults\editmode-results.xml" `
  -logFile "$PWD\TestResults\editmode-unity.log"
```

Después de terminar EditMode:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode -nographics -projectPath "$PWD" `
  -runTests -testPlatform PlayMode `
  -testResults "$PWD\TestResults\playmode-results.xml" `
  -logFile "$PWD\TestResults\playmode-unity.log"
```

Un proceso correcto devuelve `0`; el XML registra los resultados. `TestResults/` contiene artefactos locales y está ignorado por Git.

## Cobertura

| Área | Contratos principales |
| --- | --- |
| Dominio y perfil | Cálculos, precios, saldo, desbloqueos, migración y recuperación de JSON. |
| Entrada | Acciones, bindings, un comando por pulsación, dispositivos simulados y cancelación del contacto táctil. |
| Gameplay | Autoscroll sin objetivo vertical, graze, Ink-Pulse, gadgets, pausas y reintentos. |
| Escenas y prefabs | Jerarquía, referencias, áreas seguras, escala y ausencia de infraestructura duplicada. |
| Cámara y spawn | Encuadre, límites y posiciones de aparición en los formatos móviles. |
| Menús y tiendas | Navegación sin mouse, opciones por plataforma, compra, equipamiento, paginación y rechazo sin saldo. |
| Cómics y overlays | Inicio, portales, tienda, derrota y exclusión entre entrada, pausa y estados terminales. |
| Android | Atrás contextual, foco/pausa, audio, cambio de escena y manifiesto sin Internet. |

Las pruebas PlayMode utilizan el módulo UI y el PlayerLoop de Unity. Los fixtures de compra y navegación aíslan el perfil con datos transitorios y restauran el estado al terminar. Los casos de tiempo, contacto y transición están en `GameplayPauseInputPlayModeTests`, los fixtures de touch y `MobilePlatformPlayModeTests`.

La [validación de composición](SceneCompositionValidation.md) revisa el contrato de escenas y prefabs antes de compilar. Los validadores y tests de HUD comparten `MobileGameRootCatalog` para aplicar las mismas reglas a ambas zonas.

## Previsualizaciones de interfaz

Los exportadores de `Assets/Implementation/Editor/` cargan escenas y prefabs reales, aplican formatos de pantalla en memoria y generan PNG, JSON y galerías HTML bajo `TestResults/`. Requieren Edit Mode, escenas sin cambios sin guardar y un dispositivo gráfico disponible. Restauran la sesión sin guardar los cambios de previsualización.

| Método de exportación | Cobertura | Carpeta de salida |
| --- | --- | --- |
| `MobileHudPreviewExporter.Export` | HUD de ambas zonas, formatos y áreas seguras | `MobileHudPreview/` |
| `MainMenuPreviewExporter.Export` | Menú en teléfono y tablet | `MainMenuPreview/` |
| `MainMenuTutorialPreviewExporter.Export` | Tres páginas de Cómo jugar | `MainMenuTutorialPreview/` |
| `OptionsMenuPreviewExporter.Export` | Opciones en menú, tienda y zonas | `OptionsMenuPreview/` |
| `PermanentShopPreviewExporter.Export` | Controles, catálogo y textos de tienda permanente | `PermanentShopPreview/` |
| `RunMenuPreviewExporter.Export` | Pausa y Game Over de ambas zonas | `RunMenuPreview/` |
| `InGameShopPreviewExporter.Export` | Ofertas, precio, contador y aviso sin saldo | `InGameShopPreview/` |
| `LoreComicPreviewExporter.Export` | Viñetas y botón de continuación | `LoreComicPreview/` |

Están disponibles en `Tools > Squid Ink Pulse > Mobile`. En batch, usar `-executeMethod` con el método de la tabla y mantener habilitados los gráficos. Revisar las capturas junto con el JSON para detectar recortes, solapamientos y textos fuera del área segura.

## Recorrido en plataforma

Las capturas revisan composición y las pruebas simuladas revisan interacción. La comodidad, orientación, rendimiento y comportamiento de Android se comprueban en dispositivo físico. El [recorrido de entrega](03-desarrollo/pruebas-y-validacion.md) reúne los casos de Windows y Android; la [matriz móvil](MobilePort.md) detalla instalación, actualización e interrupciones.

## Servidor de feria

El complemento mantiene un smoke test separado. Con un servidor de prueba en ejecución, ejecutar desde la raíz del repositorio:

```powershell
python .\Tools\FairServer\smoke_test.py
```

La preparación del servidor y de los datos de prueba se explica en [FairServer](FairServer.md) y [la guía de evento](FairEventSetupGuide.md).

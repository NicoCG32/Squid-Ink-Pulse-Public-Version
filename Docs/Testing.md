# Pruebas automatizadas

## Alcance actual

La base inicial contiene cinco pruebas EditMode deterministas. Cubren cálculo de precios y normalización de perfil, leaderboard y mejoras permanentes. No existen todavía pruebas PlayMode ni una meta de cobertura.

Las pruebas viven en:

```text
Assets/Tests/EditMode/
```

El código runtime se compila en una única asamblea `SquidInkPulse.Runtime`. Esta frontera existe para que la asamblea de tests pueda referenciar el código de producción; no representa todavía una división arquitectónica por dominios.

## Ejecución desde Unity

1. Abrir `Window > General > Test Runner`.
2. Seleccionar `EditMode`.
3. Presionar `Run All`.

## Ejecución batch en Windows

Cerrar antes cualquier instancia de Unity que tenga abierto el proyecto y ejecutar desde la raíz del repositorio:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com' `
  -batchmode `
  -nographics `
  -projectPath "$PWD" `
  -runTests `
  -testPlatform EditMode `
  -testResults "$PWD\TestResults\editmode-results.xml" `
  -logFile "$PWD\TestResults\editmode-unity.log"
```

El proceso debe devolver código `0`. El XML informa cantidad de pruebas ejecutadas, aprobadas, fallidas y omitidas.

`TestResults/` es un artefacto generado y no debe versionarse.

## Servidor de feria

El servidor opcional mantiene su smoke test independiente:

```powershell
python Tools\FairServer\smoke_test.py
```

Este script requiere que `Tools/FairServer/server.py` esté escuchando en `127.0.0.1:8080`.

## Extensión posterior

Las siguientes incorporaciones deben priorizar caracterización de flujos que se vayan a refactorizar. No se deben añadir tests masivamente sin identificar primero qué contrato protegen.
